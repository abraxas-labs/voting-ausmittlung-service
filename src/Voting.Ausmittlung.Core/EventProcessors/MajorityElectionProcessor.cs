// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Ech.Utils;
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
    private readonly SimplePoliticalBusinessRepo _simplePoliticalBusinessRepo;
    private readonly SecondaryMajorityElectionCandidateTranslationRepo _secondaryMajorityCandidateTranslationRepo;
    private readonly MajorityElectionResultBuilder _resultBuilder;
    private readonly MajorityElectionCandidateResultBuilder _candidateResultBuilder;
    private readonly MajorityElectionEndResultInitializer _endResultInitializer;
    private readonly MajorityElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly SimplePoliticalBusinessBuilder<MajorityElection> _simplePoliticalBusinessBuilder;
    private readonly PoliticalBusinessToNewContestMover<MajorityElection, MajorityElectionRepo> _politicalBusinessToNewContestMover;
    private readonly ContestCountingCircleDetailsBuilder _contestCountingCircleDetailsBuilder;
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
        PoliticalBusinessToNewContestMover<MajorityElection, MajorityElectionRepo> politicalBusinessToNewContestMover,
        ContestCountingCircleDetailsBuilder contestCountingCircleDetailsBuilder,
        SimplePoliticalBusinessRepo simplePoliticalBusinessRepo)
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
        _contestCountingCircleDetailsBuilder = contestCountingCircleDetailsBuilder;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
    }

    public Task Process(MajorityElectionCreated eventData)
    {
        var majorityElection = MapEventData(eventData.MajorityElection);
        return CreateElection(majorityElection);
    }

    public async Task Process(MajorityElectionUpdated eventData)
    {
        var majorityElection = MapEventData(eventData.MajorityElection);
        await UpdateElection(majorityElection);

        // update secondary elections on the same ballot
        await _repo.Query()
            .Where(x => x.PrimaryMajorityElectionId == majorityElection.Id)
            .ExecuteUpdateAsync(x => x
                .SetProperty(y => y.DomainOfInfluenceId, majorityElection.DomainOfInfluenceId)
                .SetProperty(y => y.ReportDomainOfInfluenceLevel, majorityElection.ReportDomainOfInfluenceLevel)
                .SetProperty(y => y.MandateAlgorithm, majorityElection.MandateAlgorithm)
                .SetProperty(y => y.ResultEntry, majorityElection.ResultEntry)
                .SetProperty(y => y.EnforceResultEntryForCountingCircles, majorityElection.EnforceResultEntryForCountingCircles)
                .SetProperty(y => y.BallotBundleSize, majorityElection.BallotBundleSize)
                .SetProperty(y => y.BallotBundleSampleSize, majorityElection.BallotBundleSampleSize)
                .SetProperty(y => y.BallotNumberGeneration, majorityElection.BallotNumberGeneration)
                .SetProperty(y => y.AutomaticBallotBundleNumberGeneration, majorityElection.AutomaticBallotBundleNumberGeneration)
                .SetProperty(y => y.AutomaticBallotNumberGeneration, majorityElection.AutomaticBallotNumberGeneration)
                .SetProperty(y => y.AutomaticEmptyVoteCounting, majorityElection.AutomaticEmptyVoteCounting)
                .SetProperty(y => y.EnforceEmptyVoteCountingForCountingCircles, majorityElection.EnforceEmptyVoteCountingForCountingCircles)
                .SetProperty(y => y.ReviewProcedure, majorityElection.ReviewProcedure)
                .SetProperty(y => y.EnforceReviewProcedureForCountingCircles, majorityElection.EnforceReviewProcedureForCountingCircles));
    }

    public Task Process(MajorityElectionAfterTestingPhaseUpdated eventData)
        => UpdateElectionAfterTestingPhase(eventData, GuidParser.Parse(eventData.Id));

    public Task Process(MajorityElectionDeleted eventData)
        => DeleteElection(GuidParser.Parse(eventData.MajorityElectionId));

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

        var secondaryElectionsOnSeparateBallot = await _repo.QueryWithResults()
            .Include(x => x.DomainOfInfluence)
            .Where(x => x.PrimaryMajorityElectionId == id)
            .ToListAsync();
        var secondaryElectionsOnSeparateBallotIds = new HashSet<Guid>();
        foreach (var sme in secondaryElectionsOnSeparateBallot)
        {
            secondaryElectionsOnSeparateBallotIds.Add(sme.Id);
            await _politicalBusinessToNewContestMover.Move(sme, newContestId);
        }

        var simpleSecondaryElectionsOnSeparateBallot = await _simplePoliticalBusinessRepo
            .Query()
            .Include(x => x.SimpleResults)
            .ThenInclude(x => x.CountingCircle)
            .Include(x => x.DomainOfInfluence)
            .Where(x => secondaryElectionsOnSeparateBallotIds.Contains(x.Id))
            .ToListAsync();
        foreach (var sme in simpleSecondaryElectionsOnSeparateBallot)
        {
            await _simplePoliticalBusinessBuilder.MoveToNewContest(sme, newContestId);
        }
    }

    public Task Process(MajorityElectionActiveStateUpdated eventData)
        => UpdateActiveState(GuidParser.Parse(eventData.MajorityElectionId), eventData.Active);

    public Task Process(MajorityElectionCandidateCreated eventData)
        => CreateCandidate(eventData.MajorityElectionCandidate);

    public Task Process(MajorityElectionCandidateUpdated eventData)
        => UpdateCandidate(eventData.MajorityElectionCandidate);

    public Task Process(MajorityElectionCandidateAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        return UpdateCandidateAfterTestingPhase(id, eventData);
    }

    public Task Process(MajorityElectionCandidatesReordered eventData)
        => ReorderCandidates(GuidParser.Parse(eventData.MajorityElectionId), eventData.CandidateOrders);

    public async Task Process(MajorityElectionCandidateDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionCandidateId);
        await DeleteCandidate(id);
    }

    internal async Task CreateElection(MajorityElection majorityElection)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (majorityElection.ReviewProcedure == MajorityElectionReviewProcedure.Unspecified)
        {
            majorityElection.ReviewProcedure = MajorityElectionReviewProcedure.Electronically;
        }

        await _repo.Create(majorityElection);
        await _simplePoliticalBusinessBuilder.Create(majorityElection);
        await _resultBuilder.RebuildForElection(majorityElection.Id, majorityElection.DomainOfInfluenceId, false, majorityElection.ContestId);
        await _endResultInitializer.RebuildForElection(majorityElection.Id, false);
        await _contestCountingCircleDetailsBuilder.CreateMissingVotingCardsAndAggregatedDetails(majorityElection.Id, majorityElection.ContestId, majorityElection.DomainOfInfluenceId);
    }

    internal async Task UpdateElection(MajorityElection majorityElection)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (majorityElection.ReviewProcedure == MajorityElectionReviewProcedure.Unspecified)
        {
            majorityElection.ReviewProcedure = MajorityElectionReviewProcedure.Electronically;
        }

        var existingMajorityElection = await _repo.GetByKey(majorityElection.Id)
                                     ?? throw new EntityNotFoundException(majorityElection.Id);

        await _translationRepo.DeleteRelatedTranslations(majorityElection.Id);
        await _repo.Update(majorityElection);
        await _simplePoliticalBusinessBuilder.Update(majorityElection, false);

        if (majorityElection.DomainOfInfluenceId != existingMajorityElection.DomainOfInfluenceId)
        {
            await _resultBuilder.RebuildForElection(majorityElection.Id, majorityElection.DomainOfInfluenceId, false, majorityElection.ContestId);
            await _endResultInitializer.RebuildForElection(majorityElection.Id, false);
            await _contestCountingCircleDetailsBuilder.SyncForDomainOfInfluence(majorityElection.Id, majorityElection.ContestId, majorityElection.DomainOfInfluenceId);
        }

        if (majorityElection.IndividualCandidatesDisabled != existingMajorityElection.IndividualCandidatesDisabled)
        {
            await _resultBuilder.ResetIndividualVoteCounts(majorityElection.Id);
            await _endResultInitializer.ResetIndividualVoteCounts(majorityElection.Id);
        }
    }

    internal async Task UpdateElectionAfterTestingPhase<T>(T eventData, Guid id)
    {
        var majorityElection = await _repo.GetByKey(id)
                               ?? throw new EntityNotFoundException(id);
        _mapper.Map(eventData, majorityElection);

        await _translationRepo.DeleteRelatedTranslations(majorityElection.Id);
        await _repo.Update(majorityElection);
        await _simplePoliticalBusinessBuilder.Update(majorityElection, true, false);

        _logger.LogInformation("Majority election {MajorityElectionId} updated after testing phase ended", id);
    }

    internal async Task DeleteElection(Guid id)
    {
        if (!await _repo.ExistsByKey(id))
        {
            // skip event processing to prevent race condition if majority election was deleted from other process.
            _logger.LogWarning("event 'MajorityElectionDeleted' skipped. majority election {id} has already been deleted", id);
            return;
        }

        await _repo.DeleteByKey(id);
        await _simplePoliticalBusinessBuilder.Delete(id);
    }

    internal async Task UpdateActiveState(Guid majorityElectionId, bool isActive)
    {
        var existingModel = await _repo.GetByKey(majorityElectionId)
                            ?? throw new EntityNotFoundException(majorityElectionId);

        existingModel.Active = isActive;
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel, false);
    }

    internal Task CreateCandidate(MajorityElectionCandidateEventData eventData)
        => CreateCandidate(_mapper.Map<MajorityElectionCandidate>(eventData));

    internal async Task CreateCandidate(MajorityElectionCandidate candidate)
    {
        TruncateCandidateNumber(candidate);

        // old events don't contain a country
        if (string.IsNullOrEmpty(candidate.Country))
        {
            candidate.Country = CountryUtils.SwissCountryIso;
        }

        var contestState = await _repo.Query()
            .Where(x => x.Id == candidate.MajorityElectionId)
            .Select(x => x.Contest.State)
            .FirstOrDefaultAsync();

        candidate.CreatedDuringActiveContest = contestState == ContestState.Active;

        await _candidateRepo.Create(candidate);
        await _candidateResultBuilder.Initialize(candidate.MajorityElectionId, candidate.Id);
        await _candidateEndResultBuilder.Initialize(candidate.Id);
    }

    internal async Task UpdateCandidate(MajorityElectionCandidateEventData eventData)
    {
        var candidate = _mapper.Map<MajorityElectionCandidate>(eventData);
        TruncateCandidateNumber(candidate);

        // old events don't contain a country
        if (string.IsNullOrEmpty(candidate.Country))
        {
            candidate.Country = CountryUtils.SwissCountryIso;
        }

        if (!await _candidateRepo.ExistsByKey(candidate.Id))
        {
            throw new EntityNotFoundException(candidate.Id);
        }

        await _candidateTranslationRepo.DeleteRelatedTranslations(candidate.Id);
        await _candidateRepo.Update(candidate);
        await UpdateCandidateReferences(candidate);
    }

    internal async Task UpdateCandidateAfterTestingPhase<T>(Guid candidateId, T eventData)
    {
        var candidate = await _candidateRepo.GetByKey(candidateId)
                        ?? throw new EntityNotFoundException(nameof(MajorityElectionCandidate), candidateId);
        _mapper.Map(eventData, candidate);

        await _candidateTranslationRepo.DeleteRelatedTranslations(candidate.Id);
        await _candidateRepo.Update(candidate);
        await UpdateCandidateReferences(candidate);

        _logger.LogInformation("Majority election candidate {MajorityElectionCandidateId} updated after testing phase ended", candidateId);
    }

    internal async Task ReorderCandidates(Guid electionId, EntityOrdersEventData orders)
    {
        var candidates = await _candidateRepo.Query()
            .Where(c => c.MajorityElectionId == electionId)
            .ToListAsync();

        var grouped = orders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Single().Position);

        foreach (var candidate in candidates)
        {
            candidate.Position = grouped[candidate.Id];
        }

        await _candidateRepo.UpdateRange(candidates);
    }

    internal async Task DeleteCandidate(Guid id)
    {
        var existingCandidate = await _candidateRepo.GetByKey(id);
        if (existingCandidate is null)
        {
            // skip event processing to prevent race condition if majority election candidate was deleted from other process.
            _logger.LogWarning("event 'MajorityElectionCandidateDeleted' skipped. majority election candidate{id} has already been deleted", id);
            return;
        }

        await _candidateRepo.DeleteByKey(id);
        await _candidateRepo.Query()
            .Where(c => c.MajorityElectionId == existingCandidate.MajorityElectionId && c.Position > existingCandidate.Position)
            .ExecuteUpdateAsync(x => x.SetProperty(c => c.Position, c => c.Position - 1));
    }

    internal async Task UpdateReferencedCandidate(MajorityElectionCandidateReferenceEventData eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        await _candidateRepo.Query()
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x
                .SetProperty(c => c.Incumbent, eventData.Incumbent)
                .SetProperty(c => c.Number, eventData.Number)
                .SetProperty(c => c.CheckDigit, eventData.CheckDigit));
    }

    private MajorityElection MapEventData(MajorityElectionEventData majorityElectionEventData)
    {
        var majorityElection = _mapper.Map<MajorityElection>(majorityElectionEventData);
        majorityElection.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(majorityElection.ContestId, majorityElection.DomainOfInfluenceId);

        if (majorityElectionEventData.AutomaticBallotNumberGeneration == null)
        {
            majorityElection.AutomaticBallotNumberGeneration = true;
        }

        return majorityElection;
    }

    private async Task UpdateCandidateReferences(MajorityElectionCandidate candidate)
    {
        await UpdateSecondaryCandidateReferencesOnSeparateBallots(candidate);
        await UpdateSecondaryCandidateReferences(candidate);
    }

    private async Task UpdateSecondaryCandidateReferencesOnSeparateBallots(MajorityElectionCandidate candidate)
    {
        var candidateReferences = await _candidateRepo.Query()
            .Where(c => c.CandidateReferenceId == candidate.Id)
            .ToListAsync();

        foreach (var candidateReference in candidateReferences)
        {
            UpdateCandidateReference(candidate, candidateReference);
            candidateReference.Translations = candidate.Translations.Select(t => new MajorityElectionCandidateTranslation
            {
                Language = t.Language,
                Occupation = t.Occupation,
                OccupationTitle = t.OccupationTitle,
                PartyShortDescription = t.PartyShortDescription,
                PartyLongDescription = t.PartyLongDescription,
            }).ToList();
        }

        await _candidateTranslationRepo.Query()
            .Where(t => t.MajorityElectionCandidate!.CandidateReferenceId == candidate.Id)
            .ExecuteDeleteAsync();
        await _candidateRepo.UpdateRange(candidateReferences);
    }

    private async Task UpdateSecondaryCandidateReferences(MajorityElectionCandidate candidate)
    {
        var candidateReferences = await _secondaryMajorityCandidateRepo.Query()
            .Where(c => c.CandidateReferenceId == candidate.Id)
            .ToListAsync();

        if (candidateReferences.Count == 0)
        {
            return;
        }

        foreach (var candidateReference in candidateReferences)
        {
            UpdateCandidateReference(candidate, candidateReference);
            candidateReference.Translations = candidate.Translations.Select(t => new SecondaryMajorityElectionCandidateTranslation
            {
                Language = t.Language,
                Occupation = t.Occupation,
                OccupationTitle = t.OccupationTitle,
                PartyShortDescription = t.PartyShortDescription,
                PartyLongDescription = t.PartyLongDescription,
            }).ToList();
        }

        await _secondaryMajorityCandidateTranslationRepo.Query()
            .Where(t => t.SecondaryMajorityElectionCandidate!.CandidateReferenceId == candidate.Id)
            .ExecuteDeleteAsync();
        await _secondaryMajorityCandidateRepo.UpdateRange(candidateReferences);
    }

    private void UpdateCandidateReference(
        MajorityElectionCandidate candidate,
        MajorityElectionCandidateBase candidateReference)
    {
        // cannot use the mapper here, since that would overwrite some fields that should be untouched (id, position, incumbent, reporting type)
        candidateReference.FirstName = candidate.FirstName;
        candidateReference.LastName = candidate.LastName;
        candidateReference.PoliticalFirstName = candidate.PoliticalFirstName;
        candidateReference.PoliticalLastName = candidate.PoliticalLastName;
        candidateReference.Locality = candidate.Locality;
        candidateReference.DateOfBirth = candidate.DateOfBirth;
        candidateReference.Sex = candidate.Sex;
        candidateReference.Title = candidate.Title;
        candidateReference.ZipCode = candidate.ZipCode;
        candidateReference.Origin = candidate.Origin;
        candidateReference.Street = candidate.Street;
        candidateReference.HouseNumber = candidate.HouseNumber;
        candidateReference.Country = candidate.Country;
    }

    private void TruncateCandidateNumber(MajorityElectionCandidate candidate)
    {
        if (candidate.Number.Length <= 10)
        {
            return;
        }

        // old events can contain a number which is longer than 10 chars
        candidate.Number = candidate.Number[..10];
    }
}
