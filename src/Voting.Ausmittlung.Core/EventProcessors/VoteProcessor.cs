// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

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
    private readonly BallotTranslationRepo _ballotTranslationRepo;
    private readonly VoteResultRepo _voteResultsRepo;
    private readonly BallotQuestionRepo _ballotQuestionRepo;
    private readonly BallotQuestionTranslationRepo _ballotQuestionTranslationRepo;
    private readonly TieBreakQuestionRepo _tieBreakQuestionRepo;
    private readonly TieBreakQuestionTranslationRepo _tieBreakQuestionTranslationRepo;
    private readonly VoteResultBuilder _voteResultBuilder;
    private readonly VoteEndResultInitializer _voteEndResultInitializer;
    private readonly SimplePoliticalBusinessBuilder<Vote> _simplePoliticalBusinessBuilder;
    private readonly PoliticalBusinessToNewContestMover<Vote, VoteRepo> _politicalBusinessToNewContestMover;
    private readonly ILogger<VoteProcessor> _logger;
    private readonly IMapper _mapper;

    public VoteProcessor(
        ILogger<VoteProcessor> logger,
        VoteRepo repo,
        VoteTranslationRepo translationRepo,
        IDbRepository<DataContext, Ballot> ballotRepo,
        BallotTranslationRepo ballotTranslationRepo,
        VoteResultRepo voteResultsRepo,
        BallotQuestionRepo ballotQuestionRepo,
        BallotQuestionTranslationRepo ballotQuestionTranslationRepo,
        TieBreakQuestionRepo tieBreakQuestionRepo,
        TieBreakQuestionTranslationRepo tieBreakQuestionTranslationRepo,
        VoteResultBuilder voteResultBuilder,
        VoteEndResultInitializer voteEndResultInitializer,
        SimplePoliticalBusinessBuilder<Vote> simplePoliticalBusinessBuilder,
        PoliticalBusinessToNewContestMover<Vote, VoteRepo> politicalBusinessToNewContestMover,
        IMapper mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
        _politicalBusinessToNewContestMover = politicalBusinessToNewContestMover;
        _voteResultBuilder = voteResultBuilder;
        _voteEndResultInitializer = voteEndResultInitializer;
        _repo = repo;
        _translationRepo = translationRepo;
        _ballotRepo = ballotRepo;
        _ballotTranslationRepo = ballotTranslationRepo;
        _ballotQuestionRepo = ballotQuestionRepo;
        _ballotQuestionTranslationRepo = ballotQuestionTranslationRepo;
        _tieBreakQuestionRepo = tieBreakQuestionRepo;
        _tieBreakQuestionTranslationRepo = tieBreakQuestionTranslationRepo;
        _voteResultsRepo = voteResultsRepo;
    }

    public async Task Process(VoteCreated eventData)
    {
        var vote = _mapper.Map<Vote>(eventData.Vote);
        vote.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(vote.ContestId, vote.DomainOfInfluenceId);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (vote.ReviewProcedure == VoteReviewProcedure.Unspecified)
        {
            vote.ReviewProcedure = VoteReviewProcedure.Electronically;
        }

        await _repo.Create(vote);
        await _voteResultsRepo.Rebuild(vote.Id, vote.DomainOfInfluenceId, false);
        await _voteEndResultInitializer.RebuildForVote(vote.Id, false);
        await _simplePoliticalBusinessBuilder.Create(vote);
    }

    public async Task Process(VoteUpdated eventData)
    {
        var vote = _mapper.Map<Vote>(eventData.Vote);
        vote.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(vote.ContestId, vote.DomainOfInfluenceId);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (vote.ReviewProcedure == VoteReviewProcedure.Unspecified)
        {
            vote.ReviewProcedure = VoteReviewProcedure.Electronically;
        }

        var existingVote = await _repo.GetByKey(vote.Id)
                            ?? throw new EntityNotFoundException(vote.Id);

        await _translationRepo.DeleteRelatedTranslations(vote.Id);
        await _repo.Update(vote);

        if (existingVote.DomainOfInfluenceId != vote.DomainOfInfluenceId)
        {
            await _voteResultBuilder.RebuildForVote(vote.Id, vote.DomainOfInfluenceId, false);
            await _voteEndResultInitializer.RebuildForVote(vote.Id, false);
        }

        await _simplePoliticalBusinessBuilder.Update(vote, false);
    }

    public async Task Process(VoteAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var vote = await _repo.GetByKey(id)
                            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData, vote);
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
            throw new EntityNotFoundException(id);
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
    }

    public async Task Process(BallotCreated eventData)
    {
        var model = _mapper.Map<Ballot>(eventData.Ballot);

        await _ballotRepo.Create(model);
        await _voteResultBuilder.RebuildForBallot(model.Id);
        await _voteEndResultInitializer.RebuildForBallot(model.Id);
    }

    public async Task Process(BallotUpdated eventData)
    {
        var ballot = _mapper.Map<Ballot>(eventData.Ballot);

        if (!await _ballotRepo.ExistsByKey(ballot.Id))
        {
            throw new EntityNotFoundException(ballot.Id);
        }

        await _ballotTranslationRepo.DeleteRelatedTranslations(ballot.Id);
        await _ballotQuestionTranslationRepo.DeleteTranslationsByBallotId(ballot.Id);
        await _tieBreakQuestionTranslationRepo.DeleteTranslationsByBallotId(ballot.Id);

        await _ballotQuestionRepo.Replace(ballot.Id, ballot.BallotQuestions);
        await _tieBreakQuestionRepo.Replace(ballot.Id, ballot.TieBreakQuestions);
        await _ballotRepo.Update(ballot);
        await _voteResultBuilder.RebuildForBallot(ballot.Id);
        await _voteEndResultInitializer.RebuildForBallot(ballot.Id);
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

        foreach (var (lang, descTranslation) in eventData.Description)
        {
            ballot.Translations.Add(new BallotTranslation
            {
                Language = lang,
                Description = descTranslation,
            });
        }

        var updatedBallotQuestionsByNumber = eventData.BallotQuestions.ToDictionary(x => x.Number);
        foreach (var question in ballot.BallotQuestions)
        {
            foreach (var (lang, questionTranslation) in updatedBallotQuestionsByNumber[question.Number].Question)
            {
                question.Translations.Add(new BallotQuestionTranslation
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

        await _ballotTranslationRepo.DeleteRelatedTranslations(ballot.Id);
        await _ballotQuestionTranslationRepo.DeleteTranslationsByBallotId(ballot.Id);
        await _tieBreakQuestionTranslationRepo.DeleteTranslationsByBallotId(ballot.Id);
        await _ballotRepo.Update(ballot);

        _logger.LogInformation("Ballot {BallotId} updated after testing phase ended", id);
    }

    public async Task Process(BallotDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.BallotId);
        if (!await _ballotRepo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _ballotRepo.DeleteByKey(id);
    }

    public async Task Process(VoteActiveStateUpdated eventData)
    {
        var voteId = GuidParser.Parse(eventData.VoteId);
        var existingModel = await _repo.GetByKey(voteId)
                            ?? throw new EntityNotFoundException(voteId);

        existingModel.Active = eventData.Active;
        await _repo.Update(existingModel);

        await _simplePoliticalBusinessBuilder.Update(existingModel, false);
    }
}
