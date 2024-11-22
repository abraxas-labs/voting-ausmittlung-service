// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class VoteProcessor :
    IEventProcessor<VoteCreated>,
    IEventProcessor<VoteUpdated>,
    IEventProcessor<VoteAfterTestingPhaseUpdated>,
    IEventProcessor<VoteActiveStateUpdated>,
    IEventProcessor<VoteDeleted>,
    IEventProcessor<VoteToNewContestMoved>,
    IEventProcessor<BallotCreated>,
    IEventProcessor<BallotUpdated>,
    IEventProcessor<BallotAfterTestingPhaseUpdated>,
    IEventProcessor<BallotDeleted>
{
    private readonly VoteRepo _repo;
    private readonly VoteTranslationRepo _translationRepo;
    private readonly IDbRepository<DataContext, Ballot> _ballotRepo;
    private readonly VoteResultRepo _voteResultsRepo;
    private readonly BallotTranslationRepo _ballotTranslationRepo;
    private readonly IDbRepository<DataContext, BallotQuestion> _ballotQuestionRepo;
    private readonly BallotQuestionTranslationRepo _ballotQuestionTranslationRepo;
    private readonly IDbRepository<DataContext, TieBreakQuestion> _tieBreakQuestionRepo;
    private readonly TieBreakQuestionTranslationRepo _tieBreakQuestionTranslationRepo;
    private readonly VoteResultBuilder _voteResultBuilder;
    private readonly VoteEndResultInitializer _voteEndResultInitializer;
    private readonly SimplePoliticalBusinessBuilder<Vote> _simplePoliticalBusinessBuilder;
    private readonly ContestCountingCircleDetailsBuilder _contestCountingCircleDetailsBuilder;
    private readonly PoliticalBusinessToNewContestMover<Vote, VoteRepo> _politicalBusinessToNewContestMover;
    private readonly ILogger<VoteProcessor> _logger;
    private readonly IMapper _mapper;

    public VoteProcessor(
        ILogger<VoteProcessor> logger,
        VoteRepo repo,
        VoteTranslationRepo translationRepo,
        IDbRepository<DataContext, Ballot> ballotRepo,
        VoteResultRepo voteResultsRepo,
        BallotTranslationRepo ballotTranslationRepo,
        IDbRepository<DataContext, BallotQuestion> ballotQuestionRepo,
        BallotQuestionTranslationRepo ballotQuestionTranslationRepo,
        IDbRepository<DataContext, TieBreakQuestion> tieBreakQuestionRepo,
        TieBreakQuestionTranslationRepo tieBreakQuestionTranslationRepo,
        VoteResultBuilder voteResultBuilder,
        VoteEndResultInitializer voteEndResultInitializer,
        SimplePoliticalBusinessBuilder<Vote> simplePoliticalBusinessBuilder,
        ContestCountingCircleDetailsBuilder contestCountingCircleDetailsBuilder,
        PoliticalBusinessToNewContestMover<Vote, VoteRepo> politicalBusinessToNewContestMover,
        IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
        _contestCountingCircleDetailsBuilder = contestCountingCircleDetailsBuilder;
        _politicalBusinessToNewContestMover = politicalBusinessToNewContestMover;
        _voteResultBuilder = voteResultBuilder;
        _voteEndResultInitializer = voteEndResultInitializer;
        _repo = repo;
        _translationRepo = translationRepo;
        _ballotRepo = ballotRepo;
        _ballotQuestionRepo = ballotQuestionRepo;
        _ballotQuestionTranslationRepo = ballotQuestionTranslationRepo;
        _tieBreakQuestionRepo = tieBreakQuestionRepo;
        _tieBreakQuestionTranslationRepo = tieBreakQuestionTranslationRepo;
        _voteResultsRepo = voteResultsRepo;
        _ballotTranslationRepo = ballotTranslationRepo;
    }

    public async Task Process(VoteCreated eventData)
    {
        var vote = _mapper.Map<Vote>(eventData.Vote);
        vote.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(vote.ContestId, vote.DomainOfInfluenceId);

        PatchOldEventIfNecessary(vote);

        await _repo.Create(vote);
        await _simplePoliticalBusinessBuilder.Create(vote);
        await _voteResultsRepo.Rebuild(vote.Id, vote.DomainOfInfluenceId, false, vote.ContestId);
        await _voteEndResultInitializer.RebuildForVote(vote.Id, false);
        await _contestCountingCircleDetailsBuilder.SyncForDomainOfInfluence(vote.Id, vote.ContestId, vote.DomainOfInfluenceId);
    }

    public async Task Process(VoteUpdated eventData)
    {
        var vote = _mapper.Map<Vote>(eventData.Vote);
        vote.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(vote.ContestId, vote.DomainOfInfluenceId);

        PatchOldEventIfNecessary(vote);

        var existingVote = await _repo.GetByKey(vote.Id)
            ?? throw new EntityNotFoundException(vote.Id);
        await CalculateSubTypeForVoteWithoutFetchedBallots(vote);

        await _translationRepo.DeleteRelatedTranslations(vote.Id);
        await _repo.Update(vote);
        await _simplePoliticalBusinessBuilder.Update(vote, false);

        if (existingVote.DomainOfInfluenceId != vote.DomainOfInfluenceId)
        {
            await _voteResultBuilder.RebuildForVote(vote.Id, vote.DomainOfInfluenceId, false, vote.ContestId);
            await _voteEndResultInitializer.RebuildForVote(vote.Id, false);
            await _contestCountingCircleDetailsBuilder.SyncForDomainOfInfluence(vote.Id, vote.ContestId, vote.DomainOfInfluenceId);
        }
    }

    public async Task Process(VoteAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var vote = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData, vote);
        await CalculateSubTypeForVoteWithoutFetchedBallots(vote);

        await _translationRepo.DeleteRelatedTranslations(vote.Id);
        await _repo.Update(vote);

        await _simplePoliticalBusinessBuilder.Update(vote, true, false);

        _logger.LogInformation("Vote {VoteId} updated after testing phase ended", id);
    }

    public async Task Process(VoteDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.VoteId);
        if (!await _repo.ExistsByKey(id))
        {
            // skip event processing to prevent race condition if vote was deleted from other process.
            _logger.LogWarning("event 'VoteDeleted' skipped. vote {id} has already been deleted", id);
            return;
        }

        await _repo.DeleteByKey(id);
        await _simplePoliticalBusinessBuilder.Delete(id);
    }

    public async Task Process(VoteToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.VoteId);
        var newContestId = GuidParser.Parse(eventData.NewContestId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _politicalBusinessToNewContestMover.Move(id, newContestId);
        await _simplePoliticalBusinessBuilder.MoveToNewContest(id, newContestId);
        await UpdateVoteSubTypeIfNecessary(id);
    }

    public async Task Process(BallotCreated eventData)
    {
        var model = _mapper.Map<Ballot>(eventData.Ballot);

        SetDefaultValues(model);

        await _ballotRepo.Create(model);
        await _voteResultBuilder.RebuildForBallot(model.Id);
        await _voteEndResultInitializer.RebuildForBallot(model.Id);
        await UpdateVoteSubTypeIfNecessary(model.VoteId);
    }

    public async Task Process(BallotUpdated eventData)
    {
        var ballot = _mapper.Map<Ballot>(eventData.Ballot);

        if (!await _ballotRepo.ExistsByKey(ballot.Id))
        {
            throw new EntityNotFoundException(ballot.Id);
        }

        SetDefaultValues(ballot);

        await _ballotTranslationRepo.DeleteRelatedTranslations(ballot.Id);
        await _ballotQuestionTranslationRepo.DeleteTranslationsByBallotId(ballot.Id);
        await _tieBreakQuestionTranslationRepo.DeleteTranslationsByBallotId(ballot.Id);

        var existingBallotQuestions = await _ballotQuestionRepo.Query()
            .Where(x => x.BallotId == ballot.Id)
            .ToListAsync();

        var ballotQuestionsByNumber = ballot.BallotQuestions.ToDictionary(x => x.Number);

        var ballotQuestionsToRemove = new List<Guid>();
        foreach (var existingBallotQuestion in existingBallotQuestions)
        {
            if (!ballotQuestionsByNumber.TryGetValue(existingBallotQuestion.Number, out var ballotQuestion))
            {
                ballotQuestionsToRemove.Add(existingBallotQuestion.Id);
                continue;
            }

            ballotQuestion.Id = existingBallotQuestion.Id;
        }

        await _ballotQuestionRepo.DeleteRangeByKey(ballotQuestionsToRemove);

        var existingTieBreakQuestions = await _tieBreakQuestionRepo.Query()
            .Where(x => x.BallotId == ballot.Id)
            .ToListAsync();

        var tieBreakQuestionsByNumber = ballot.TieBreakQuestions.ToDictionary(x => x.Number);

        var tieBreakQuestionsToRemove = new List<Guid>();
        foreach (var existingTieBreakQuestion in existingTieBreakQuestions)
        {
            if (!tieBreakQuestionsByNumber.TryGetValue(existingTieBreakQuestion.Number, out var tieBreakQuestion))
            {
                tieBreakQuestionsToRemove.Add(existingTieBreakQuestion.Id);
                continue;
            }

            tieBreakQuestion.Id = existingTieBreakQuestion.Id;
        }

        await _tieBreakQuestionRepo.DeleteRangeByKey(tieBreakQuestionsToRemove);

        await _ballotRepo.Update(ballot);
        await _voteResultBuilder.RebuildForBallot(ballot.Id);
        await _voteEndResultInitializer.RebuildForBallot(ballot.Id);
        await UpdateVoteSubTypeIfNecessary(ballot.VoteId);
    }

    public async Task Process(BallotAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var ballot = await _ballotRepo.Query()
            .AsSplitQuery()
            .Include(b => b.BallotQuestions)
            .Include(b => b.TieBreakQuestions)
            .Include(b => b.Vote)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new EntityNotFoundException(id);

        var updatedBallotQuestionsByNumber = eventData.BallotQuestions.ToDictionary(x => x.Number);
        foreach (var existingQuestion in ballot.BallotQuestions)
        {
            var question = updatedBallotQuestionsByNumber[existingQuestion.Number];
            existingQuestion.Type = _mapper.Map<BallotQuestionType>(question.Type);
            foreach (var (lang, questionTranslation) in question.Question)
            {
                existingQuestion.Translations.Add(new BallotQuestionTranslation
                {
                    Language = lang,
                    Question = questionTranslation,
                });
            }
        }

        var updatedTieBreakQuestionsByNumber = eventData.TieBreakQuestions.ToDictionary(x => x.Number);
        foreach (var tieBreakQuestion in ballot.TieBreakQuestions)
        {
            foreach (var (lang, questionTranslation) in updatedTieBreakQuestionsByNumber[tieBreakQuestion.Number].Question)
            {
                tieBreakQuestion.Translations.Add(new TieBreakQuestionTranslation
                {
                    Language = lang,
                    Question = questionTranslation,
                });
            }
        }

        SetDefaultValues(ballot);

        await _ballotQuestionTranslationRepo.DeleteTranslationsByBallotId(ballot.Id);
        await _tieBreakQuestionTranslationRepo.DeleteTranslationsByBallotId(ballot.Id);

        await _ballotRepo.Update(ballot);
        await UpdateVoteSubTypeIfNecessary(ballot.VoteId);

        _logger.LogInformation("Ballot {BallotId} updated after testing phase ended", id);
    }

    public async Task Process(BallotDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.BallotId);
        if (!await _ballotRepo.ExistsByKey(id))
        {
            // skip event processing to prevent race condition if ballot was deleted from other process.
            _logger.LogWarning("event 'BallotDeleted' skipped. ballot {id} has already been deleted", id);
            return;
        }

        await _ballotRepo.DeleteByKey(id);
        await UpdateVoteSubTypeIfNecessary(Guid.Parse(eventData.VoteId));
    }

    public async Task Process(VoteActiveStateUpdated eventData)
    {
        var voteId = GuidParser.Parse(eventData.VoteId);
        var existingModel = await _repo.GetByKey(voteId)
                            ?? throw new EntityNotFoundException(voteId);

        existingModel.Active = eventData.Active;
        await CalculateSubTypeForVoteWithoutFetchedBallots(existingModel);
        await _repo.Update(existingModel);

        await _simplePoliticalBusinessBuilder.Update(existingModel, false);
    }

    private async Task CalculateSubTypeForVoteWithoutFetchedBallots(Vote vote)
    {
        var hasBallotWithVariantBallotType = await _ballotRepo.Query()
            .AnyAsync(x => x.VoteId == vote.Id && x.BallotType == BallotType.VariantsBallot);
        vote.UpdateSubTypeManually(hasBallotWithVariantBallotType);
    }

    private void PatchOldEventIfNecessary(Vote vote)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (vote.ReviewProcedure == VoteReviewProcedure.Unspecified)
        {
            vote.ReviewProcedure = VoteReviewProcedure.Electronically;
        }

        if (vote.Type == VoteType.Unspecified)
        {
            vote.Type = VoteType.QuestionsOnSingleBallot;
        }
    }

    private async Task UpdateVoteSubTypeIfNecessary(Guid voteId)
    {
        var voteInfo = await _repo.Query()
            .Where(x => x.Id == voteId)
            .Select(x => new
            {
                Vote = x,
                HasBallotWithVariantBallotType = x.Ballots.Any(b => b.BallotType == BallotType.VariantsBallot),
            })
            .FirstAsync();
        voteInfo.Vote.UpdateSubTypeManually(voteInfo.HasBallotWithVariantBallotType);
        await _simplePoliticalBusinessBuilder.UpdateSubTypeIfNecessary(voteInfo.Vote);
    }

    private void SetDefaultValues(Ballot ballot)
    {
        // Set default ballot question type value since the old eventData (before introducing the type) can contain the unspecified value.
        foreach (var ballotQuestion in ballot.BallotQuestions)
        {
            if (ballotQuestion.Type == BallotQuestionType.Unspecified)
            {
                ballotQuestion.Type = ballotQuestion.Number == 1
                    ? BallotQuestionType.MainBallot
                    : BallotQuestionType.CounterProposal;
            }
        }
    }
}
