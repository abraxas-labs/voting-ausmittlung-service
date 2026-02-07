// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Core.Services.Read;

public class MajorityElectionResultBundleReader
{
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBallot> _ballotRepo;
    private readonly PoliticalBusinessResultBundleBuilder _politicalBusinessResultBundleBuilder;
    private readonly PermissionService _permissionService;
    private readonly IAuth _auth;

    public MajorityElectionResultBundleReader(
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, MajorityElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, MajorityElectionResultBallot> ballotRepo,
        PermissionService permissionService,
        IAuth auth,
        PoliticalBusinessResultBundleBuilder politicalBusinessResultBundleBuilder)
    {
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _permissionService = permissionService;
        _auth = auth;
        _politicalBusinessResultBundleBuilder = politicalBusinessResultBundleBuilder;
    }

    public async Task<MajorityElectionResult> GetElectionResultWithBundles(Guid electionResultId)
    {
        var electionResult = await _resultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle)
            .Include(x => x.MajorityElection.Translations)
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.MajorityElection.Contest.Translations)
            .Include(x => x.MajorityElection.Contest.CantonDefaults)
            .Include(x => x.Bundles.OrderBy(b => b.Number))
            .ThenInclude(x => x.Logs.OrderBy(y => y.Timestamp))
            .FirstOrDefaultAsync(x => x.Id == electionResultId)
            ?? throw new EntityNotFoundException(electionResultId);

        await _permissionService.EnsureCanReadCountingCircle(electionResult.CountingCircleId, electionResult.MajorityElection.ContestId);

        var countOfModifiedBallotsByBundleId = await _bundleRepo.Query()
            .Where(b => b.ElectionResultId == electionResultId)
            .Select(b => new
            {
                b.Id,
                CountOfModifiedBallots = b.Ballots.Count(ballot => ballot.Logs.Count > 0),
            })
            .ToDictionaryAsync(x => x.Id, x => x.CountOfModifiedBallots);
        foreach (var bundle in electionResult.Bundles)
        {
            if (countOfModifiedBallotsByBundleId.TryGetValue(bundle.Id, out var countOfModifiedBallots))
            {
                bundle.CountOfModifiedBallots = countOfModifiedBallots;
            }
        }

        await _politicalBusinessResultBundleBuilder.AddProtocolExportsToBundles(
            electionResult.Bundles,
            electionResult.CountingCircle.BasisCountingCircleId,
            electionResult.PoliticalBusinessId,
            electionResult.MajorityElection.ContestId,
            electionResult.MajorityElection.Contest.TestingPhaseEnded,
            AusmittlungPdfMajorityElectionTemplates.ResultBundleReview.Key);

        return electionResult;
    }

    public async Task<MajorityElectionResultBundle> GetBundle(Guid bundleId)
    {
        var bundle = await _bundleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ElectionResult.CountingCircle)
            .Include(x => x.ElectionResult.MajorityElection.Translations)
            .Include(x => x.ElectionResult.MajorityElection.Contest.Translations)
            .Include(x => x.ElectionResult.MajorityElection.Contest.CantonDefaults)
            .Include(x => x.ElectionResult.MajorityElection.DomainOfInfluence)
            .Include(x => x.ElectionResult.SecondaryMajorityElectionResults).ThenInclude(r => r.SecondaryMajorityElection.Translations)
            .Include(x => x.Logs.OrderBy(y => y.Timestamp))
            .FirstOrDefaultAsync(x => x.Id == bundleId)
            ?? throw new EntityNotFoundException(bundleId);

        await _permissionService.EnsureCanReadCountingCircle(bundle.ElectionResult.CountingCircleId, bundle.ElectionResult.MajorityElection.ContestId);

        var ballotInfo = await _ballotRepo.Query()
            .Where(b => b.BundleId == bundle.Id)
            .Select(b => new
            {
                b.Number,
                b.MarkedForReview,
                b.Index,
                HasLogs = b.Logs.Count > 0,
            })
            .OrderBy(b => b.Index)
            .ToListAsync();
        bundle.BallotNumbers = ballotInfo.ConvertAll(b => b.Number);
        bundle.BallotNumbersToReview = ballotInfo
            .Where(b => b.MarkedForReview)
            .Select(b => b.Number)
            .ToList();
        bundle.CountOfModifiedBallots = ballotInfo.Count(b => b.HasLogs);

        return bundle;
    }

    public async Task<MajorityElectionResultBallot> GetBallot(Guid bundleId, int ballotNumber)
    {
        var ballot = await _ballotRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Bundle.ElectionResult.MajorityElection)
            .Include(x => x.BallotCandidates).ThenInclude(c => c.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionBallots).ThenInclude(x => x.SecondaryMajorityElectionResult.SecondaryMajorityElection)
            .Include(x => x.SecondaryMajorityElectionBallots).ThenInclude(x => x.BallotCandidates).ThenInclude(c => c.Candidate.Translations)
            .Include(x => x.Logs.OrderBy(y => y.Timestamp))
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

        // Users with this permission are always able to view the result ballot
        if (_auth.HasPermission(Permissions.PoliticalBusinessResultBallot.ReadAll))
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
