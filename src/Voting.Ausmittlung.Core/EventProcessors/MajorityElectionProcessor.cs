// (c) Copyright 2024 by Abraxas Informatik AG
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

public class MajorityElectionProcessor :
    IEventProcessor<MajorityElectionCreated>,
    IEventProcessor<MajorityElectionUpdated>,
    IEventProcessor<MajorityElectionAfterTestingPhaseUpdated>,
    IEventProcessor<MajorityElectionActiveStateUpdated>,
    IEventProcessor<MajorityElectionDeleted>,
    IEventProcessor<MajorityElectionToNewContestMoved>,
    IEventProcessor<MajorityElectionCandidateCreated>,
    IEventProcessor<MajorityElectionCandidateUpdated>,
    IEventProcessor<MajorityElectionCandidateAfterTestingPhaseUpdated>,
    IEventProcessor<MajorityElectionCandidatesReordered>,
    IEventProcessor<MajorityElectionCandidateDeleted>
{
    private readonly MajorityElectionRepo _repo;
    private readonly MajorityElectionTranslationRepo _translationRepo;
    private readonly IDbRepository<DataContext, MajorityElectionCandidate> _candidateRepo;
    private readonly MajorityElectionCandidateTranslationRepo _candidateTranslationRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionCandidate> _secondaryMajorityCandidateRepo;
    private readonly SecondaryMajorityElectionCandidateTranslationRepo _secondaryMajorityCandidateTranslationRepo;
    private readonly MajorityElectionResultBuilder _resultBuilder;
    private readonly MajorityElectionCandidateResultBuilder _candidateResultBuilder;
    private readonly MajorityElectionEndResultInitializer _endResultInitializer;
    private readonly MajorityElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly SimplePoliticalBusinessBuilder<MajorityElection> _simplePoliticalBusinessBuilder;
    private readonly PoliticalBusinessToNewContestMover<MajorityElection, MajorityElectionRepo> _politicalBusinessToNewContestMover;
    private readonly ILogger<MajorityElectionProcessor> _logger;
    private readonly IMapper _mapper;

    public MajorityElectionProcessor(
        ILogger<MajorityElectionProcessor> logger,
        IMapper mapper,
        MajorityElectionRepo repo,
        MajorityElectionTranslationRepo translationRepo,
        IDbRepository<DataContext, MajorityElectionCandidate> candidateRepo,
        MajorityElectionCandidateTranslationRepo candidateTranslationRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionCandidate> secondaryMajorityCandidateRepo,
        SecondaryMajorityElectionCandidateTranslationRepo secondaryMajorityCandidateTranslationRepo,
        MajorityElectionResultBuilder resultBuilder,
        MajorityElectionCandidateResultBuilder candidateResultBuilder,
        MajorityElectionEndResultInitializer endResultInitializer,
        MajorityElectionCandidateEndResultBuilder candidateEndResultBuilder,
        SimplePoliticalBusinessBuilder<MajorityElection> simplePoliticalBusinessBuilder,
        PoliticalBusinessToNewContestMover<MajorityElection, MajorityElectionRepo> politicalBusinessToNewContestMover)
    {
        _logger = logger;
        _mapper = mapper;
        _repo = repo;
        _translationRepo = translationRepo;
        _candidateRepo = candidateRepo;
        _candidateTranslationRepo = candidateTranslationRepo;
        _secondaryMajorityCandidateRepo = secondaryMajorityCandidateRepo;
        _secondaryMajorityCandidateTranslationRepo = secondaryMajorityCandidateTranslationRepo;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
        _candidateResultBuilder = candidateResultBuilder;
        _endResultInitializer = endResultInitializer;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _resultBuilder = resultBuilder;
        _politicalBusinessToNewContestMover = politicalBusinessToNewContestMover;
    }

    public async Task Process(MajorityElectionCreated eventData)
    {
        var majorityElection = _mapper.Map<MajorityElection>(eventData.MajorityElection);

        majorityElection.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(majorityElection.ContestId, majorityElection.DomainOfInfluenceId);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (majorityElection.ReviewProcedure == MajorityElectionReviewProcedure.Unspecified)
        {
            majorityElection.ReviewProcedure = MajorityElectionReviewProcedure.Electronically;
        }

        await _repo.Create(majorityElection);
        await _resultBuilder.RebuildForElection(majorityElection.Id, majorityElection.DomainOfInfluenceId, false);
        await _endResultInitializer.RebuildForElection(majorityElection.Id, false);
        await _simplePoliticalBusinessBuilder.Create(majorityElection);
    }

    public async Task Process(MajorityElectionUpdated eventData)
    {
        var majorityElection = _mapper.Map<MajorityElection>(eventData.MajorityElection);
        majorityElection.DomainOfInfluenceId =
            AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(majorityElection.ContestId, majorityElection.DomainOfInfluenceId);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (majorityElection.ReviewProcedure == MajorityElectionReviewProcedure.Unspecified)
        {
            majorityElection.ReviewProcedure = MajorityElectionReviewProcedure.Electronically;
        }

        var existingMajorityElection = await _repo.GetByKey(majorityElection.Id)
            ?? throw new EntityNotFoundException(majorityElection.Id);

        await _translationRepo.DeleteRelatedTranslations(majorityElection.Id);
        await _repo.Update(majorityElection);

        if (majorityElection.DomainOfInfluenceId != existingMajorityElection.DomainOfInfluenceId)
        {
            await _resultBuilder.RebuildForElection(majorityElection.Id, majorityElection.DomainOfInfluenceId, false);
            await _endResultInitializer.RebuildForElection(majorityElection.Id, false);
        }

        await _simplePoliticalBusinessBuilder.Update(majorityElection, false);
    }

    public async Task Process(MajorityElectionAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var majorityElection = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);
        _mapper.Map(eventData, majorityElection);

        await _translationRepo.DeleteRelatedTranslations(majorityElection.Id);
        await _repo.Update(majorityElection);
        await _simplePoliticalBusinessBuilder.Update(majorityElection, true, false);

        _logger.LogInformation("Majority election {MajorityElectionId} updated after testing phase ended", id);
    }

    public async Task Process(MajorityElectionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _repo.DeleteByKey(id);
        await _simplePoliticalBusinessBuilder.Delete(id);
    }

    public async Task Process(MajorityElectionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionId);
        var newContestId = GuidParser.Parse(eventData.NewContestId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _politicalBusinessToNewContestMover.Move(id, newContestId);
        await _simplePoliticalBusinessBuilder.MoveToNewContest(id, newContestId);
    }

    public async Task Process(MajorityElectionActiveStateUpdated eventData)
    {
        var majorityElectionId = GuidParser.Parse(eventData.MajorityElectionId);
        var existingModel = await _repo.GetByKey(majorityElectionId)
            ?? throw new EntityNotFoundException(majorityElectionId);

        existingModel.Active = eventData.Active;
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel, false);
    }

    public async Task Process(MajorityElectionCandidateCreated eventData)
    {
        var model = _mapper.Map<MajorityElectionCandidate>(eventData.MajorityElectionCandidate);
        await _candidateRepo.Create(model);
        await _candidateResultBuilder.Initialize(model.MajorityElectionId, model.Id);
        await _candidateEndResultBuilder.Initialize(model.Id);
    }

    public async Task Process(MajorityElectionCandidateUpdated eventData)
    {
        var candidate = _mapper.Map<MajorityElectionCandidate>(eventData.MajorityElectionCandidate);

        if (!await _candidateRepo.ExistsByKey(candidate.Id))
        {
            throw new EntityNotFoundException(candidate.Id);
        }

        await _candidateTranslationRepo.DeleteRelatedTranslations(candidate.Id);
        await _candidateRepo.Update(candidate);
        await UpdateCandidateReferences(candidate);
    }

    public async Task Process(MajorityElectionCandidateAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var candidate = await _candidateRepo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);
        _mapper.Map(eventData, candidate);

        await _candidateTranslationRepo.DeleteRelatedTranslations(candidate.Id);
        await _candidateRepo.Update(candidate);
        await UpdateCandidateReferences(candidate);

        _logger.LogInformation("Majority election candidate {MajorityElectionCandidateId} updated after testing phase ended", id);
    }

    public async Task Process(MajorityElectionCandidatesReordered eventData)
    {
        var majorityElectionId = GuidParser.Parse(eventData.MajorityElectionId);

        var candidates = await _candidateRepo.Query()
            .Where(c => c.MajorityElectionId == majorityElectionId)
            .ToListAsync();

        var grouped = eventData.CandidateOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Single().Position);

        foreach (var candidate in candidates)
        {
            candidate.Position = grouped[candidate.Id];
        }

        await _candidateRepo.UpdateRange(candidates);
    }

    public async Task Process(MajorityElectionCandidateDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionCandidateId);

        var existingCandidate = await _candidateRepo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        await _candidateRepo.DeleteByKey(id);

        var candidatesToUpdate = await _candidateRepo.Query()
            .Where(c => c.MajorityElectionId == existingCandidate.MajorityElectionId
                && c.Position > existingCandidate.Position)
            .ToListAsync();

        foreach (var candidate in candidatesToUpdate)
        {
            candidate.Position--;
        }

        await _candidateRepo.UpdateRange(candidatesToUpdate);
    }

    private async Task UpdateCandidateReferences(MajorityElectionCandidate candidate)
    {
        var candidateReferences = await _secondaryMajorityCandidateRepo.Query()
            .Where(c => c.CandidateReferenceId == candidate.Id)
            .ToListAsync();

        foreach (var candidateReference in candidateReferences)
        {
            // cannot use the mapper here, since that would overwrite some fields that should be untouched (id, position, incumbent)
            candidateReference.FirstName = candidate.FirstName;
            candidateReference.LastName = candidate.LastName;
            candidateReference.PoliticalFirstName = candidate.PoliticalFirstName;
            candidateReference.PoliticalLastName = candidate.PoliticalLastName;
            candidateReference.Locality = candidate.Locality;
            candidateReference.Number = candidate.Number;
            candidateReference.DateOfBirth = candidate.DateOfBirth;
            candidateReference.Sex = candidate.Sex;
            candidateReference.Title = candidate.Title;
            candidateReference.ZipCode = candidate.ZipCode;
            candidateReference.Origin = candidate.Origin;
            candidate.Translations = candidate.Translations.Select(t => new MajorityElectionCandidateTranslation
            {
                Language = t.Language,
                Occupation = t.Occupation,
                OccupationTitle = t.OccupationTitle,
                Party = t.Party,
            }).ToList();
        }

        await _secondaryMajorityCandidateTranslationRepo.DeleteCandidateReferenceTranslations(candidate.Id);
        await _secondaryMajorityCandidateRepo.UpdateRange(candidateReferences);
    }
}
