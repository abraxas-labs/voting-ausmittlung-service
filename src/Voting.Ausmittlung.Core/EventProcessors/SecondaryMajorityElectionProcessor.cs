// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Ech.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class SecondaryMajorityElectionProcessor :
    IEventProcessor<SecondaryMajorityElectionCreated>,
    IEventProcessor<SecondaryMajorityElectionUpdated>,
    IEventProcessor<SecondaryMajorityElectionAfterTestingPhaseUpdated>,
    IEventProcessor<SecondaryMajorityElectionDeleted>,
    IEventProcessor<SecondaryMajorityElectionActiveStateUpdated>,
    IEventProcessor<SecondaryMajorityElectionCandidateCreated>,
    IEventProcessor<SecondaryMajorityElectionCandidateUpdated>,
    IEventProcessor<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated>,
    IEventProcessor<SecondaryMajorityElectionCandidateDeleted>,
    IEventProcessor<SecondaryMajorityElectionCandidatesReordered>,
    IEventProcessor<SecondaryMajorityElectionCandidateReferenceCreated>,
    IEventProcessor<SecondaryMajorityElectionCandidateReferenceUpdated>,
    IEventProcessor<SecondaryMajorityElectionCandidateReferenceDeleted>
{
    private readonly ILogger<SecondaryMajorityElectionProcessor> _logger;
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _repo;
    private readonly SecondaryMajorityElectionTranslationRepo _translationRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionCandidate> _candidateRepo;
    private readonly SecondaryMajorityElectionCandidateTranslationRepo _candidateTranslationRepo;
    private readonly IDbRepository<DataContext, MajorityElectionCandidate> _majorityElectionCandidateRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;
    private readonly IDbRepository<DataContext, ElectionGroup> _electionGroupRepo;
    private readonly MajorityElectionResultBuilder _resultBuilder;
    private readonly MajorityElectionCandidateResultBuilder _candidateResultBuilder;
    private readonly MajorityElectionEndResultInitializer _endResultInitializer;
    private readonly MajorityElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly SimplePoliticalBusinessBuilder<SecondaryMajorityElection> _simplePoliticalBusinessBuilder;
    private readonly MajorityElectionProcessor _primaryMajorityElectionProcessor;
    private readonly IMapper _mapper;

    public SecondaryMajorityElectionProcessor(
        ILogger<SecondaryMajorityElectionProcessor> logger,
        IDbRepository<DataContext, SecondaryMajorityElection> repo,
        SecondaryMajorityElectionTranslationRepo translationRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionCandidate> candidateRepo,
        SecondaryMajorityElectionCandidateTranslationRepo candidateTranslationRepo,
        IDbRepository<DataContext, MajorityElectionCandidate> majorityElectionCandidateRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        IDbRepository<DataContext, ElectionGroup> electionGroupRepo,
        MajorityElectionResultBuilder resultBuilder,
        MajorityElectionCandidateResultBuilder candidateResultBuilder,
        MajorityElectionEndResultInitializer endResultInitializer,
        MajorityElectionCandidateEndResultBuilder candidateEndResultBuilder,
        SimplePoliticalBusinessBuilder<SecondaryMajorityElection> simplePoliticalBusinessBuilder,
        IMapper mapper,
        MajorityElectionProcessor primaryMajorityElectionProcessor)
    {
        _logger = logger;
        _repo = repo;
        _translationRepo = translationRepo;
        _candidateRepo = candidateRepo;
        _candidateTranslationRepo = candidateTranslationRepo;
        _majorityElectionCandidateRepo = majorityElectionCandidateRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _electionGroupRepo = electionGroupRepo;
        _candidateResultBuilder = candidateResultBuilder;
        _resultBuilder = resultBuilder;
        _endResultInitializer = endResultInitializer;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _mapper = mapper;
        _primaryMajorityElectionProcessor = primaryMajorityElectionProcessor;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
    }

    public async Task Process(SecondaryMajorityElectionCreated eventData)
    {
        if (eventData.SecondaryMajorityElection.IsOnSeparateBallot)
        {
            await CreateMajorityElectionOnSeparateBallot(eventData);
            return;
        }

        var model = _mapper.Map<SecondaryMajorityElection>(eventData.SecondaryMajorityElection);

        var primaryElection = await _majorityElectionRepo.Query()
                                  .AsTracking()
                                  .Where(me => me.Id == model.PrimaryMajorityElectionId)
                                  .Include(x => x.ElectionGroup)
                                  .Include(x => x.Contest)
                                  .FirstOrDefaultAsync()
                              ?? throw new EntityNotFoundException(model.PrimaryMajorityElectionId);
        primaryElection.ElectionGroup!.CountOfSecondaryElections++;
        model.ElectionGroupId = primaryElection.ElectionGroup.Id;
        model.PrimaryMajorityElection = primaryElection;

        await _electionGroupRepo.Update(primaryElection.ElectionGroup);
        await _repo.Create(model);
        await _resultBuilder.InitializeSecondaryElection(model.PrimaryMajorityElectionId, model.Id, primaryElection.Contest.TestingPhaseEnded);
        await _endResultInitializer.InitializeForSecondaryElection(model.Id);
        await _simplePoliticalBusinessBuilder.Create(model);
        await _simplePoliticalBusinessBuilder.AdjustCountOfSecondaryBusinesses(model.PrimaryMajorityElectionId, 1);
    }

    public async Task Process(SecondaryMajorityElectionUpdated eventData)
    {
        if (eventData.SecondaryMajorityElection.IsOnSeparateBallot)
        {
            await UpdateMajorityElectionOnSeparateBallot(eventData);
            return;
        }

        var model = _mapper.Map<SecondaryMajorityElection>(eventData.SecondaryMajorityElection);

        var existing = await _repo.Query()
                           .AsTracking()
                           .Include(x => x.PrimaryMajorityElection) // needed for the political business builder
                           .FirstOrDefaultAsync(x => x.Id == model.Id)
                       ?? throw new EntityNotFoundException(model.Id);

        model.ElectionGroupId = existing.ElectionGroupId;
        model.PrimaryMajorityElection = existing.PrimaryMajorityElection;

        await _translationRepo.DeleteRelatedTranslations(model.Id);
        await _repo.Update(model);
        await _simplePoliticalBusinessBuilder.Update(model, false);

        if (model.IndividualCandidatesDisabled != existing.IndividualCandidatesDisabled)
        {
            await _resultBuilder.ResetSecondaryIndividualVoteCounts(model.Id);
            await _endResultInitializer.ResetSecondaryIndividualVoteCounts(model.Id);
        }
    }

    public async Task Process(SecondaryMajorityElectionAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        if (eventData.IsOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.UpdateElectionAfterTestingPhase(eventData, id);
            return;
        }

        var sme = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        // For backwards compability, we treat a missing political business number as no change to the field
        if (eventData.PoliticalBusinessNumber == string.Empty)
        {
            eventData.PoliticalBusinessNumber = sme.PoliticalBusinessNumber;
        }

        _mapper.Map(eventData, sme);

        await _translationRepo.DeleteRelatedTranslations(id);
        await _repo.Update(sme);

        // needed for the political business builder and event log
        sme.PrimaryMajorityElection = await _majorityElectionRepo.GetByKey(sme.PrimaryMajorityElectionId)
            ?? throw new EntityNotFoundException(sme.PrimaryMajorityElectionId);
        await _simplePoliticalBusinessBuilder.Update(sme, true, false);

        _logger.LogInformation("Secondary majority election {SecondaryMajorityElectionId} updated after testing phase ended", id);
    }

    public async Task Process(SecondaryMajorityElectionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        if (eventData.IsOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.DeleteElection(GuidParser.Parse(eventData.SecondaryMajorityElectionId));
            return;
        }

        var electionGroup = await _repo.Query()
            .Where(e => e.Id == id)
            .Select(e => e.ElectionGroup)
            .FirstOrDefaultAsync() ?? throw new EntityNotFoundException(id);

        electionGroup.CountOfSecondaryElections--;
        await _electionGroupRepo.Update(electionGroup);
        await _repo.DeleteByKey(id);
        await _simplePoliticalBusinessBuilder.Delete(id);
        await _simplePoliticalBusinessBuilder.AdjustCountOfSecondaryBusinesses(electionGroup.PrimaryMajorityElectionId, -1);
    }

    public async Task Process(SecondaryMajorityElectionActiveStateUpdated eventData)
    {
        if (eventData.IsOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.UpdateActiveState(
                GuidParser.Parse(eventData.SecondaryMajorityElectionId),
                eventData.Active);
            return;
        }

        var smeId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var existingModel = await _repo.Query()
                                .Include(x => x.PrimaryMajorityElection)
                                .FirstOrDefaultAsync(x => x.Id == smeId)
                            ?? throw new EntityNotFoundException(smeId);

        existingModel.Active = eventData.Active;
        await _repo.Update(existingModel);
        await _simplePoliticalBusinessBuilder.Update(existingModel, false);
    }

    public async Task Process(SecondaryMajorityElectionCandidateCreated eventData)
    {
        if (eventData.IsOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.CreateCandidate(eventData.SecondaryMajorityElectionCandidate);
            return;
        }

        var model = _mapper.Map<SecondaryMajorityElectionCandidate>(eventData.SecondaryMajorityElectionCandidate);
        TruncateCandidateNumber(model);

        // old events don't contain a country
        if (string.IsNullOrEmpty(model.Country))
        {
            model.Country = CountryUtils.SwissCountryIso;
        }

        var majorityElection = await _repo.Query()
                .Include(x => x.PrimaryMajorityElection.Contest)
                .FirstOrDefaultAsync(x => x.Id == model.SecondaryMajorityElectionId)
            ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElection), model.SecondaryMajorityElectionId);

        model.CreatedDuringActiveContest = majorityElection.Contest.State == ContestState.Active;

        await _candidateRepo.Create(model);
        await _candidateResultBuilder.InitializeSecondaryMajorityElectionCandidate(model.SecondaryMajorityElectionId, model.Id);
        await _candidateEndResultBuilder.InitializeForSecondaryMajorityElectionCandidate(model.Id);
    }

    public async Task Process(SecondaryMajorityElectionCandidateUpdated eventData)
    {
        if (eventData.IsOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.UpdateCandidate(eventData.SecondaryMajorityElectionCandidate);
            return;
        }

        var model = _mapper.Map<SecondaryMajorityElectionCandidate>(eventData.SecondaryMajorityElectionCandidate);
        TruncateCandidateNumber(model);

        // old events don't contain a country
        if (string.IsNullOrEmpty(model.Country))
        {
            model.Country = CountryUtils.SwissCountryIso;
        }

        if (!await _candidateRepo.ExistsByKey(model.Id))
        {
            throw new EntityNotFoundException(model.Id);
        }

        await _candidateTranslationRepo.DeleteRelatedTranslations(model.Id);
        await _candidateRepo.Update(model);
    }

    public async Task Process(SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated eventData)
    {
        if (eventData.IsOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.UpdateCandidateAfterTestingPhase(
                GuidParser.Parse(eventData.Id),
                eventData);
            return;
        }

        var id = GuidParser.Parse(eventData.Id);
        var candidate = await _candidateRepo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData, candidate);
        await _candidateTranslationRepo.DeleteRelatedTranslations(candidate.Id);
        await _candidateRepo.Update(candidate);

        _logger.LogInformation("Secondary majority election candidate {SecondaryMajorityElectionCandidateId} updated after testing phase ended", id);
    }

    public Task Process(SecondaryMajorityElectionCandidateDeleted eventData)
        => DeleteCandidate(eventData.IsOnSeparateBallot, eventData.SecondaryMajorityElectionCandidateId);

    public async Task Process(SecondaryMajorityElectionCandidatesReordered eventData)
    {
        if (eventData.IsOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.ReorderCandidates(GuidParser.Parse(eventData.SecondaryMajorityElectionId), eventData.CandidateOrders);
            return;
        }

        var secondaryMajorityElectionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);

        var candidates = await _candidateRepo.Query()
            .Where(c => c.SecondaryMajorityElectionId == secondaryMajorityElectionId)
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

    public async Task Process(SecondaryMajorityElectionCandidateReferenceCreated eventData)
    {
        var referencedCandidateId = GuidParser.Parse(eventData.MajorityElectionCandidateReference.CandidateId);
        var referencedCandidate = await _majorityElectionCandidateRepo.Query()
            .IgnoreQueryFilters() // do not filter translations
            .Include(x => x.Translations)
            .FirstAsync(x => x.Id == referencedCandidateId);

        if (eventData.MajorityElectionCandidateReference.IsOnSeparateBallot)
        {
            var referencedCandidateCopy = _mapper.Map<MajorityElectionCandidate>(referencedCandidate);

            // reset translation ids
            referencedCandidateCopy.Translations = referencedCandidateCopy.Translations.Select(t => new MajorityElectionCandidateTranslation
            {
                Language = t.Language,
                Occupation = t.Occupation,
                OccupationTitle = t.OccupationTitle,
                PartyShortDescription = t.PartyShortDescription,
                PartyLongDescription = t.PartyLongDescription,
            }).ToList();
            _mapper.Map(eventData.MajorityElectionCandidateReference, referencedCandidateCopy);
            await _primaryMajorityElectionProcessor.CreateCandidate(referencedCandidateCopy);
            return;
        }

        var contestState = await _repo.Query()
            .Where(x => x.Id == referencedCandidate.MajorityElectionId)
            .Select(x => x.Contest.State)
            .FirstOrDefaultAsync();
        var candidateReference = _mapper.Map<SecondaryMajorityElectionCandidate>(referencedCandidate);
        _mapper.Map(eventData.MajorityElectionCandidateReference, candidateReference);
        candidateReference.CreatedDuringActiveContest = contestState == ContestState.Active;

        await _candidateRepo.Create(candidateReference);
        await _candidateResultBuilder.InitializeSecondaryMajorityElectionCandidate(candidateReference.SecondaryMajorityElectionId, candidateReference.Id);
        await _candidateEndResultBuilder.InitializeForSecondaryMajorityElectionCandidate(candidateReference.Id);
    }

    public async Task Process(SecondaryMajorityElectionCandidateReferenceUpdated eventData)
    {
        if (eventData.MajorityElectionCandidateReference.IsOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.UpdateReferencedCandidate(eventData.MajorityElectionCandidateReference);
            return;
        }

        var id = GuidParser.Parse(eventData.MajorityElectionCandidateReference.Id);
        await _candidateRepo.Query()
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x
                .SetProperty(c => c.Incumbent, eventData.MajorityElectionCandidateReference.Incumbent)
                .SetProperty(c => c.Number, eventData.MajorityElectionCandidateReference.Number)
                .SetProperty(c => c.CheckDigit, eventData.MajorityElectionCandidateReference.CheckDigit)
                .SetProperty(c => c.ReportingType, _mapper.Map<MajorityElectionCandidateReportingType>(eventData.MajorityElectionCandidateReference.ReportingType)));
    }

    public async Task Process(SecondaryMajorityElectionCandidateReferenceDeleted eventData)
    {
        await DeleteCandidate(eventData.IsOnSeparateBallot, eventData.SecondaryMajorityElectionCandidateReferenceId);
    }

    private async Task DeleteCandidate(bool isOnSeparateBallot, string candidateId)
    {
        var id = GuidParser.Parse(candidateId);
        if (isOnSeparateBallot)
        {
            await _primaryMajorityElectionProcessor.DeleteCandidate(id);
            return;
        }

        var existingCandidate = await _candidateRepo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);
        await _candidateRepo.DeleteByKey(id);
        await _candidateRepo.Query()
            .Where(c => c.SecondaryMajorityElectionId == existingCandidate.SecondaryMajorityElectionId
                && c.Position > existingCandidate.Position)
            .ExecuteUpdateAsync(x => x.SetProperty(c => c.Position, c => c.Position - 1));
    }

    private void TruncateCandidateNumber(SecondaryMajorityElectionCandidate candidate)
    {
        if (candidate.Number.Length <= 10)
        {
            return;
        }

        // old events can contain a number which is longer than 10 chars
        candidate.Number = candidate.Number[..10];
    }

    private async Task CreateMajorityElectionOnSeparateBallot(SecondaryMajorityElectionCreated eventData)
    {
        var primaryElectionId = GuidParser.Parse(eventData.SecondaryMajorityElection.PrimaryMajorityElectionId);
        var primaryElection = await _majorityElectionRepo.GetByKey(primaryElectionId)
                              ?? throw new EntityNotFoundException(nameof(MajorityElection), primaryElectionId);
        var majorityElection = _mapper.Map<MajorityElection>(primaryElection);
        _mapper.Map(eventData.SecondaryMajorityElection, majorityElection);
        await _primaryMajorityElectionProcessor.CreateElection(majorityElection);
    }

    private async Task UpdateMajorityElectionOnSeparateBallot(SecondaryMajorityElectionUpdated eventData)
    {
        var primaryElectionId = GuidParser.Parse(eventData.SecondaryMajorityElection.PrimaryMajorityElectionId);
        var primaryElection = await _majorityElectionRepo.GetByKey(primaryElectionId)
                              ?? throw new EntityNotFoundException(nameof(MajorityElection), primaryElectionId);
        var majorityElection = _mapper.Map<MajorityElection>(primaryElection);
        _mapper.Map(eventData.SecondaryMajorityElection, majorityElection);
        await _primaryMajorityElectionProcessor.UpdateElection(majorityElection);
    }
}
