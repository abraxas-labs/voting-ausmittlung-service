// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.Services.Read;

public class MajorityElectionResultBundleReader
{
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBallot> _ballotRepo;
    private readonly MessageConsumerHub<MajorityElectionBundleChanged, MajorityElectionResultBundle> _bundleChangeListener;
    private readonly PermissionService _permissionService;

    public MajorityElectionResultBundleReader(
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, MajorityElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, MajorityElectionResultBallot> ballotRepo,
        PermissionService permissionService,
        MessageConsumerHub<MajorityElectionBundleChanged, MajorityElectionResultBundle> bundleChangeListener)
    {
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _permissionService = permissionService;
        _bundleChangeListener = bundleChangeListener;
    }

    public async Task<MajorityElectionResult> GetElectionResultWithBundles(Guid electionResultId)
    {
        _permissionService.EnsureAnyRole();
        var electionResult = await _resultRepo.Query()
                                 .AsSplitQuery()
                                 .Include(x => x.CountingCircle)
                                 .Include(x => x.MajorityElection.Translations)
                                 .Include(x => x.MajorityElection.DomainOfInfluence)
                                 .Include(x => x.MajorityElection.Contest.Translations)
                                 .Include(x => x.Bundles.OrderBy(b => b.Number))
                                 .FirstOrDefaultAsync(x => x.Id == electionResultId)
                             ?? throw new EntityNotFoundException(electionResultId);

        await _permissionService.EnsureCanReadCountingCircle(electionResult.CountingCircleId, electionResult.MajorityElection.ContestId);

        return electionResult;
    }

    public async Task ListenToBundleChanges(
        Guid electionResultId,
        Func<MajorityElectionResultBundle, Task> listener,
        CancellationToken cancellationToken)
    {
        _permissionService.EnsureAnyRole();
        var data = await _resultRepo.Query()
                                 .Where(x => x.Id == electionResultId)
                                 .Select(x => new { x.CountingCircleId, x.MajorityElection.ContestId })
                                 .FirstOrDefaultAsync(cancellationToken)
                             ?? throw new EntityNotFoundException(electionResultId);

        await _permissionService.EnsureCanReadCountingCircle(data.CountingCircleId, data.ContestId);

        await _bundleChangeListener.Listen(
            b => b.ElectionResultId == electionResultId,
            listener,
            cancellationToken);
    }

    public async Task<MajorityElectionResultBundle> GetBundle(Guid bundleId)
    {
        _permissionService.EnsureAnyRole();
        var bundle = await _bundleRepo.Query()
                                 .AsSplitQuery()
                                 .Include(x => x.ElectionResult.CountingCircle)
                                 .Include(x => x.ElectionResult.MajorityElection.Translations)
                                 .Include(x => x.ElectionResult.MajorityElection.Contest.Translations)
                                 .Include(x => x.ElectionResult.MajorityElection.DomainOfInfluence)
                                 .Include(x => x.ElectionResult.MajorityElection.Contest.DomainOfInfluence)
                                 .Include(x => x.ElectionResult.SecondaryMajorityElectionResults).ThenInclude(r => r.SecondaryMajorityElection.Translations)
                                 .FirstOrDefaultAsync(x => x.Id == bundleId)
                             ?? throw new EntityNotFoundException(bundleId);

        await _permissionService.EnsureCanReadCountingCircle(bundle.ElectionResult.CountingCircleId, bundle.ElectionResult.MajorityElection.ContestId);

        bundle.BallotNumbersToReview = await _ballotRepo.Query()
            .Where(b => b.BundleId == bundle.Id && b.MarkedForReview)
            .Select(b => b.Number)
            .OrderBy(b => b)
            .ToListAsync();

        return bundle;
    }

    public async Task<MajorityElectionResultBallot> GetBallot(Guid bundleId, int ballotNumber)
    {
        _permissionService.EnsureAnyRole();
        var ballot = await _ballotRepo.Query()
                         .AsSplitQuery()
                         .Include(x => x.Bundle.ElectionResult.MajorityElection)
                         .Include(x => x.BallotCandidates).ThenInclude(c => c.Candidate.Translations)
                         .Include(x => x.SecondaryMajorityElectionBallots).ThenInclude(x => x.SecondaryMajorityElectionResult.SecondaryMajorityElection)
                         .Include(x => x.SecondaryMajorityElectionBallots).ThenInclude(x => x.BallotCandidates).ThenInclude(c => c.Candidate.Translations)
                         .FirstOrDefaultAsync(x => x.BundleId == bundleId && x.Number == ballotNumber)
                     ?? throw new EntityNotFoundException(new { bundleId, ballotNumber });

        await EnsureCanReadBallot(ballot);

        ballot.BallotCandidates = ballot.BallotCandidates
            .OrderBy(c => c.Candidate.Position)
            .ToList();

        ballot.SecondaryMajorityElectionBallots = ballot.SecondaryMajorityElectionBallots
            .OrderBy(x => x.SecondaryMajorityElectionResult.SecondaryMajorityElection.PoliticalBusinessNumber)
            .ToList();

        foreach (var secondaryBallot in ballot.SecondaryMajorityElectionBallots)
        {
            secondaryBallot.BallotCandidates = secondaryBallot.BallotCandidates
                .OrderBy(c => c.Candidate.Position)
                .ToList();
        }

        return ballot;
    }

    private async Task EnsureCanReadBallot(MajorityElectionResultBallot ballot)
    {
        // The current tenant must have read permission on the counting circle where the ballot result comes from
        await _permissionService.EnsureCanReadCountingCircle(
            ballot.Bundle.ElectionResult.CountingCircleId,
            ballot.Bundle.ElectionResult.MajorityElection.ContestId);

        // These roles are always able to view the result ballot
        if (_permissionService.IsErfassungElectionAdmin() || _permissionService.IsMonitoringElectionAdmin())
        {
            return;
        }

        // The original creator is always able to view the result ballot
        if (ballot.Bundle.CreatedBy.SecureConnectId == _permissionService.UserId)
        {
            return;
        }

        // For reviewing a result ballot, other users must be able to view the result ballot
        if (ballot.Bundle.State == BallotBundleState.ReadyForReview && ballot.MarkedForReview)
        {
            return;
        }

        throw new ForbiddenException();
    }
}
