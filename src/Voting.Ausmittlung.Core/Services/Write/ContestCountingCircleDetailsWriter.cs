// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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
using Voting.Ausmittlung.Data.Repositories;
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
    private readonly ContestCountingCircleDetailsRepo _ccDetailsRepo;
    private readonly ContestService _contestService;
    private readonly ValidationResultsEnsurer _validationResultsEnsurer;

    public ContestCountingCircleDetailsWriter(
        ILogger<ContestCountingCircleDetailsWriter> logger,
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        PermissionService permissionService,
        IDbRepository<DataContext, DataModels.CountingCircle> countingCircleRepo,
        ContestCountingCircleDetailsRepo ccDetailsRepo,
        ContestService contestService,
        ValidationResultsEnsurer validationResultsEnsurer)
    {
        _logger = logger;
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _permissionService = permissionService;
        _countingCircleRepo = countingCircleRepo;
        _ccDetailsRepo = ccDetailsRepo;
        _contestService = contestService;
        _validationResultsEnsurer = validationResultsEnsurer;
    }

    public async Task CreateOrUpdate(ContestCountingCircleDetails details)
    {
        _permissionService.EnsureErfassungElectionAdmin();
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(details.CountingCircleId, details.ContestId);
        var (_, testingPhaseEnded) = await _contestService.EnsureNotLocked(details.ContestId);
        await ValidateSwissAbroadsDetailsOnlyIfAllowed(details.ContestId, details.CountingCircleId, details);

        // The ContestCountingCircleDetails from the request do not contain immutable data such as the eVoting flag -> need to fetch it
        var existingCcDetails = await GetDetails(details.ContestId, details.CountingCircleId, testingPhaseEnded);
        await EnsureValidVotingCards(details.ContestId, details.CountingCircleId, details, existingCcDetails.EVoting);

        await _validationResultsEnsurer.EnsureContestCountingCircleDetailsIsValid(details, existingCcDetails);

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

    private async Task ValidateSwissAbroadsDetailsOnlyIfAllowed(
        Guid contestId,
        Guid basisCountingCircleId,
        ContestCountingCircleDetails details)
    {
        if (details.CountOfVotersInformation.SubTotalInfo.All(x => x.VoterType != DataModels.VoterType.SwissAbroad))
        {
            return;
        }

        var swissAbroadsOnEveryCountingCircle = await _countingCircleRepo
            .Query()
            .Where(cc => cc.BasisCountingCircleId == basisCountingCircleId && cc.SnapshotContestId == contestId)
            .SelectMany(cc => cc.VoteResults
                .Select(vr => new { vr.Vote.ContestId, vr.Vote.DomainOfInfluence.CantonDefaults.SwissAbroadVotingRight })
                .Concat(cc.ProportionalElectionResults.Select(vr => new { vr.ProportionalElection.ContestId, vr.ProportionalElection.DomainOfInfluence.CantonDefaults.SwissAbroadVotingRight }))
                .Concat(cc.MajorityElectionResults.Select(vr => new { vr.MajorityElection.ContestId, vr.MajorityElection.DomainOfInfluence.CantonDefaults.SwissAbroadVotingRight })))
            .AnyAsync(x => x.ContestId == contestId
                           && x.SwissAbroadVotingRight == DataModels.SwissAbroadVotingRight.OnEveryCountingCircle);

        if (!swissAbroadsOnEveryCountingCircle)
        {
            throw new ValidationException("swiss abroads not allowed");
        }
    }

    private async Task EnsurePoliticalBusinessesStillInProgress(Guid contestId, Guid basisCountingCircleId)
    {
        var hasFinishedPoliticalBusiness = await _countingCircleRepo
            .Query()
            .Where(cc => cc.BasisCountingCircleId == basisCountingCircleId && cc.SnapshotContestId == contestId)
            .SelectMany(cc => cc.VoteResults
                .Select(br => br.State)
                .Concat(cc.ProportionalElectionResults.Select(pr => pr.State))
                .Concat(cc.MajorityElectionResults.Select(mr => mr.State)))
            .AnyAsync(state => state >= DataModels.CountingCircleResultState.SubmissionDone);

        if (hasFinishedPoliticalBusiness)
        {
            throw new ValidationException("A political business is already finished, cannot update the contest counting circle details.");
        }
    }

    private async Task EnsureValidVotingCards(
        Guid contestId,
        Guid basisCountingCircleId,
        ContestCountingCircleDetails details,
        bool eVotingEnabled)
    {
        var providedDomainOfInfluenceTypes = details.VotingCards.Select(vc => vc.DomainOfInfluenceType).ToHashSet();

        var cc = await _countingCircleRepo
                     .Query()
                     .AsSplitQuery()
                     .Include(x => x.SnapshotContest!.DomainOfInfluence.CantonDefaults)
                     .Include(x => x.SimpleResults).ThenInclude(x => x.PoliticalBusiness!).ThenInclude(x => x.DomainOfInfluence)
                     .FirstOrDefaultAsync(cc => cc.BasisCountingCircleId == basisCountingCircleId && cc.SnapshotContestId == contestId)
                 ?? throw new EntityNotFoundException(new { basisCountingCircleId, contestId });

        var domainOfInfluenceTypes = cc.SimpleResults.Select(spb => spb.PoliticalBusiness!).Select(pb => pb.DomainOfInfluence.Type).Distinct();

        var hasInvalidDomainOfInfluenceType = providedDomainOfInfluenceTypes.Except(domainOfInfluenceTypes).Any();
        if (hasInvalidDomainOfInfluenceType)
        {
            throw new ValidationException("Voting cards with domain of influence type which don't exist are provided.");
        }

        var enabledChannels = cc.SnapshotContest!
            .DomainOfInfluence
            .CantonDefaults
            .EnabledVotingCardChannels
            .Select(x => (x.Valid, x.Channel))
            .ToHashSet();

        // E-Voting is not explicitely set as an enabled voting channel on the canton defaults, since it can vary between contests
        // Add it here if it is enabled for this contest
        if (eVotingEnabled)
        {
            enabledChannels.Add((Valid: true, Channel: DataModels.VotingChannel.EVoting));
        }

        var invalidVotingCardChannel = details.VotingCards.Find(x => !enabledChannels.Contains((x.Valid, x.Channel)));
        if (invalidVotingCardChannel != null)
        {
            throw new ValidationException($"Voting card channel {invalidVotingCardChannel.Channel}/{invalidVotingCardChannel.Valid} is not enabled");
        }
    }

    private async Task<DataModels.ContestCountingCircleDetails> GetDetails(Guid contestId, Guid ccId, bool testingPhaseEnded)
    {
        var ccDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, ccId, testingPhaseEnded);

        return await _ccDetailsRepo.GetWithRelatedEntities(ccDetailsId)
          ?? throw new EntityNotFoundException(new { contestId, ccId });
    }
}
