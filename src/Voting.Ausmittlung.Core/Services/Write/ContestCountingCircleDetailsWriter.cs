// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ContestCountingCircleDetailsWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ILogger<ContestCountingCircleDetailsWriter> _logger;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, DataModels.CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, DataModels.Contest> _contestRepo;
    private readonly ContestService _contestService;

    public ContestCountingCircleDetailsWriter(
        ILogger<ContestCountingCircleDetailsWriter> logger,
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        PermissionService permissionService,
        IDbRepository<DataContext, DataModels.CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, DataModels.Contest> contestRepo,
        ContestService contestService,
        ValidationResultsEnsurer validationResultsEnsurer)
    {
        _logger = logger;
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _permissionService = permissionService;
        _countingCircleRepo = countingCircleRepo;
        _contestRepo = contestRepo;
        _contestService = contestService;
    }

    public async Task CreateOrUpdate(ContestCountingCircleDetails details)
    {
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(details.CountingCircleId, details.ContestId);
        var (_, testingPhaseEnded) = await _contestService.EnsureNotLocked(details.ContestId);
        await ValidateCountOfVoters(details.ContestId, details.CountingCircleId, details);

        await EnsureValidVotingCards(details.ContestId, details.CountingCircleId, details);
        await EnsureValidCountingMachine(details.ContestId, details.CountingMachine);
        await EnsureValidDetailsInElectorates(details);

        var id = AusmittlungUuidV5.BuildContestCountingCircleDetails(details.ContestId, details.CountingCircleId, testingPhaseEnded);
        var aggregate = await _aggregateRepository.TryGetById<ContestCountingCircleDetailsAggregate>(id);
        if (aggregate == null)
        {
            aggregate = _aggregateFactory.New<ContestCountingCircleDetailsAggregate>();
            aggregate.CreateFrom(details, details.ContestId, details.CountingCircleId, testingPhaseEnded);
            _logger.LogInformation("Creating contest counting circle details for {ContestCountingCircleDetailsId}", id);
        }
        else
        {
            aggregate.UpdateFrom(details, details.ContestId, details.CountingCircleId);
            _logger.LogInformation("Updating contest counting circle details for {ContestCountingCircleDetailsId}", id);
        }

        await EnsurePoliticalBusinessesStillInProgress(details.ContestId, details.CountingCircleId);

        await _aggregateRepository.Save(aggregate);
    }

    private async Task ValidateCountOfVoters(
        Guid contestId,
        Guid basisCountingCircleId,
        ContestCountingCircleDetails details)
    {
        var subTotalInfos = details.CountOfVotersInformationSubTotals;

        if (subTotalInfos.All(x => x.VoterType == DataModels.VoterType.Swiss))
        {
            return;
        }

        var domainOfInfluences = await _countingCircleRepo
            .Query()
            .Where(cc => cc.BasisCountingCircleId == basisCountingCircleId && cc.SnapshotContestId == contestId)
            .SelectMany(cc => cc.VoteResults
                .Select(vr => new { vr.Vote.ContestId, vr.Vote.DomainOfInfluence.SwissAbroadVotingRight, vr.Vote.DomainOfInfluence.HasForeignerVoters, vr.Vote.DomainOfInfluence.HasMinorVoters })
                .Concat(cc.ProportionalElectionResults.Select(vr => new { vr.ProportionalElection.ContestId, vr.ProportionalElection.DomainOfInfluence.SwissAbroadVotingRight, vr.ProportionalElection.DomainOfInfluence.HasForeignerVoters, vr.ProportionalElection.DomainOfInfluence.HasMinorVoters }))
                .Concat(cc.MajorityElectionResults.Select(vr => new { vr.MajorityElection.ContestId, vr.MajorityElection.DomainOfInfluence.SwissAbroadVotingRight, vr.MajorityElection.DomainOfInfluence.HasForeignerVoters, vr.MajorityElection.DomainOfInfluence.HasMinorVoters })))
            .Where(x => x.ContestId == contestId)
            .ToListAsync();

        if (!domainOfInfluences.Any(x => x.SwissAbroadVotingRight == DataModels.SwissAbroadVotingRight.OnEveryCountingCircle)
            && subTotalInfos.Any(x => x.VoterType == DataModels.VoterType.SwissAbroad))
        {
            throw new ValidationException("swiss abroads not allowed");
        }

        if (!domainOfInfluences.Any(x => x.HasForeignerVoters) && subTotalInfos.Any(x => x.VoterType == DataModels.VoterType.Foreigner))
        {
            throw new ValidationException("foreigners not allowed");
        }

        if (!domainOfInfluences.Any(x => x.HasMinorVoters) && subTotalInfos.Any(x => x.VoterType == DataModels.VoterType.Minor))
        {
            throw new ValidationException("minors not allowed");
        }
    }

    private async Task EnsurePoliticalBusinessesStillInProgress(Guid contestId, Guid basisCountingCircleId)
    {
        var resultStates = await _countingCircleRepo
            .Query()
            .Where(cc => cc.BasisCountingCircleId == basisCountingCircleId && cc.SnapshotContestId == contestId)
            .SelectMany(cc => cc.VoteResults
                .Select(br => br.State)
                .Concat(cc.ProportionalElectionResults.Select(pr => pr.State))
                .Concat(cc.MajorityElectionResults.Select(mr => mr.State)))
            .ToListAsync();

        if (resultStates.Count == 0)
        {
            throw new ValidationException("Counting circle has no results, cannot update the contest counting circle details.");
        }

        var hasFinishedPoliticalBusiness = resultStates.Any(state => state >= DataModels.CountingCircleResultState.SubmissionDone);

        if (hasFinishedPoliticalBusiness)
        {
            throw new ValidationException("A political business is already finished, cannot update the contest counting circle details.");
        }
    }

    private async Task EnsureValidVotingCards(
        Guid contestId,
        Guid basisCountingCircleId,
        ContestCountingCircleDetails details)
    {
        // Remove the e-voting voting cards, as they are immutable (only updated via e-voting import)
        details.VotingCards = details.VotingCards
            .Where(x => x.Channel != DataModels.VotingChannel.EVoting)
            .ToList();

        var providedDomainOfInfluenceTypes = details.VotingCards.Select(vc => vc.DomainOfInfluenceType).ToHashSet();

        var cc = await _countingCircleRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.SnapshotContest!.CantonDefaults)
            .FirstOrDefaultAsync(cc => cc.BasisCountingCircleId == basisCountingCircleId && cc.SnapshotContestId == contestId)
            ?? throw new EntityNotFoundException(new { basisCountingCircleId, contestId });

        var enabledChannels = cc.SnapshotContest!
            .CantonDefaults
            .EnabledVotingCardChannels
            .Select(x => (x.Valid, x.Channel))
            .ToHashSet();

        var invalidVotingCardChannel = details.VotingCards.Find(x => !enabledChannels.Contains((x.Valid, x.Channel)));
        if (invalidVotingCardChannel != null)
        {
            throw new ValidationException($"Voting card channel {invalidVotingCardChannel.Channel}/{invalidVotingCardChannel.Valid} is not enabled");
        }
    }

    private async Task EnsureValidCountingMachine(Guid contestId, DataModels.CountingMachine countingMachine)
    {
        var countingMachineEnabled = await _contestRepo.Query()
            .Include(c => c.CantonDefaults)
            .AnyAsync(c => c.Id == contestId && c.CantonDefaults.CountingMachineEnabled);

        if (!countingMachineEnabled && countingMachine != DataModels.CountingMachine.Unspecified)
        {
            throw new ValidationException("Cannot set counting machine if it is not enabled on canton settings");
        }
    }

    private async Task EnsureValidDetailsInElectorates(ContestCountingCircleDetails details)
    {
        var basisCountingCircleId = details.CountingCircleId;
        var contestId = details.ContestId;

        var cc = await _countingCircleRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.SimpleResults).ThenInclude(x => x.PoliticalBusiness!).ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.Electorates)
            .Include(x => x.ContestElectorates)
            .FirstOrDefaultAsync(cc => cc.BasisCountingCircleId == basisCountingCircleId && cc.SnapshotContestId == contestId)
            ?? throw new EntityNotFoundException(new { basisCountingCircleId, contestId });

        var providedDoiTypes = details.VotingCards.Select(vc => vc.DomainOfInfluenceType)
            .Concat(details.CountOfVotersInformationSubTotals.Select(st => st.DomainOfInfluenceType))
            .ToHashSet();

        var validDoiTypes = cc.SimpleResults.Select(spb => spb.PoliticalBusiness!).Select(pb => pb.DomainOfInfluence.Type).Distinct();
        if (providedDoiTypes.Except(validDoiTypes).Any())
        {
            throw new ValidationException("Voting cards or count of voters information sub totals with domain of influence type which don't exist are provided.");
        }

        var electorates = BuildElectorates(cc, providedDoiTypes);

        foreach (var electorate in electorates)
        {
            var uniqueVotingCardCountsByChannelAndValid = details.VotingCards
                .Where(vc => electorate.DomainOfInfluenceTypes.Contains(vc.DomainOfInfluenceType))
                .GroupBy(vc => new { vc.Channel, vc.Valid })
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.CountOfReceivedVotingCards).ToHashSet());

            if (uniqueVotingCardCountsByChannelAndValid.Any(e => e.Value.Count > 1))
            {
                throw new ValidationException("Voting card counts per electorate, channel and valid state must be unique");
            }

            var uniqueCountOfVotersInformationSubTotalCount = details.CountOfVotersInformationSubTotals
                .Where(st => electorate.DomainOfInfluenceTypes.Contains(st.DomainOfInfluenceType))
                .GroupBy(st => new { st.VoterType, st.Sex, st.DomainOfInfluenceType })
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.CountOfVoters).ToHashSet());

            if (uniqueCountOfVotersInformationSubTotalCount.Any(e => e.Value.Count > 1))
            {
                throw new ValidationException("Count of voters information sub total per electorate, voter type and sex must be unique");
            }
        }
    }

    private List<DataModels.CountingCircleElectorateBase> BuildElectorates(DataModels.CountingCircle cc, HashSet<DataModels.DomainOfInfluenceType> providedDoiTypes)
    {
        var electorates = cc.ContestElectorates.Count > 0
            ? cc.ContestElectorates.OfType<DataModels.CountingCircleElectorateBase>().ToList()
            : cc.Electorates.OfType<DataModels.CountingCircleElectorateBase>().ToList();

        var electorateDoiTypes = electorates.SelectMany(e => e.DomainOfInfluenceTypes).ToHashSet();
        var providedUnelectoratedDoiTypes = providedDoiTypes.Where(doiType => !electorateDoiTypes.Contains(doiType)).ToList();
        if (providedUnelectoratedDoiTypes.Count > 0)
        {
            electorates.Add(new DataModels.CountingCircleElectorate { DomainOfInfluenceTypes = providedUnelectoratedDoiTypes });
        }

        return electorates;
    }
}
