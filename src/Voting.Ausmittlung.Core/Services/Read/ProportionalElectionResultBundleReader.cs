// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Messaging;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ProportionalElectionResultBundleReader
{
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepo;
    private readonly LanguageAwareMessageConsumerHub<ProportionalElectionBundleChanged, ProportionalElectionResultBundle> _bundleChangeListener;
    private readonly PermissionService _permissionService;
    private readonly IAuth _auth;
    private readonly LanguageService _languageService;
    private readonly PoliticalBusinessResultBundleBuilder _politicalBusinessResultBundleBuilder;

    public ProportionalElectionResultBundleReader(
        IDbRepository<DataContext, ProportionalElectionResult> resultRepo,
        IDbRepository<DataContext, ProportionalElectionResultBundle> bundleRepo,
        IDbRepository<DataContext, ProportionalElectionResultBallot> ballotRepo,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        LanguageAwareMessageConsumerHub<ProportionalElectionBundleChanged, ProportionalElectionResultBundle> bundleChangeListener,
        PermissionService permissionService,
        IAuth auth,
        LanguageService languageService,
        PoliticalBusinessResultBundleBuilder politicalBusinessResultBundleBuilder)
    {
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _protocolExportRepo = protocolExportRepo;
        _permissionService = permissionService;
        _auth = auth;
        _languageService = languageService;
        _politicalBusinessResultBundleBuilder = politicalBusinessResultBundleBuilder;
        _bundleChangeListener = bundleChangeListener;
    }

    public async Task<ProportionalElectionResult> GetElectionResultWithBundles(Guid electionResultId)
    {
        var electionResult = await _resultRepo.Query()
                                 .AsSplitQuery()
                                 .Include(x => x.CountingCircle)
                                 .Include(x => x.ProportionalElection.Translations)
                                 .Include(x => x.ProportionalElection.DomainOfInfluence)
                                 .Include(x => x.ProportionalElection.Contest.Translations)
                                 .Include(x => x.ProportionalElection.Contest.CantonDefaults)
                                 .Include(x => x.Bundles).ThenInclude(x => x.List!.Translations)
                                 .FirstOrDefaultAsync(x => x.Id == electionResultId)
                             ?? throw new EntityNotFoundException(electionResultId);

        await _permissionService.EnsureCanReadCountingCircle(electionResult.CountingCircleId, electionResult.ProportionalElection.ContestId);

        electionResult.Bundles = electionResult.Bundles
            .OrderBy(b => b.Number)
            .ToList();

        await _politicalBusinessResultBundleBuilder.AddProtocolExportsToBundles(
            electionResult.Bundles,
            electionResult.CountingCircle.BasisCountingCircleId,
            electionResult.PoliticalBusinessId,
            electionResult.ProportionalElection.ContestId,
            electionResult.ProportionalElection.Contest.TestingPhaseEnded,
            AusmittlungPdfProportionalElectionTemplates.ResultBundleReview.Key);

        return electionResult;
    }

    public async Task<ProportionalElectionResultBundle> GetBundle(Guid bundleId)
    {
        var bundle = await _bundleRepo.Query()
                                 .AsSplitQuery()
                                 .Include(x => x.ElectionResult.CountingCircle)
                                 .Include(x => x.ElectionResult.ProportionalElection.DomainOfInfluence)
                                 .Include(x => x.ElectionResult.ProportionalElection.Translations)
                                 .Include(x => x.ElectionResult.ProportionalElection.Contest.Translations)
                                 .Include(x => x.ElectionResult.ProportionalElection.Contest.CantonDefaults)
                                 .Include(x => x.List!.Translations)
                                 .FirstOrDefaultAsync(x => x.Id == bundleId)
                             ?? throw new EntityNotFoundException(bundleId);

        await _permissionService.EnsureCanReadCountingCircle(bundle.ElectionResult.CountingCircleId, bundle.ElectionResult.ProportionalElection.ContestId);

        bundle.BallotNumbersToReview = await _ballotRepo.Query()
            .Where(b => b.BundleId == bundle.Id && b.MarkedForReview)
            .Select(b => b.Number)
            .OrderBy(b => b)
            .ToListAsync();

        return bundle;
    }

    public async Task ListenToBundleChanges(
        Guid electionResultId,
        Func<ProportionalElectionResultBundle, Task> listener,
        CancellationToken cancellationToken)
    {
        var data = await _resultRepo.Query()
                       .Where(x => x.Id == electionResultId)
                       .Select(x => new { x.CountingCircleId, x.ProportionalElection.ContestId })
                       .FirstOrDefaultAsync(cancellationToken)
                   ?? throw new EntityNotFoundException(electionResultId);

        await _permissionService.EnsureCanReadCountingCircle(data.CountingCircleId, data.ContestId);

        await _bundleChangeListener.Listen(
            b => b.ElectionResultId == electionResultId,
            listener,
            _languageService.Language,
            cancellationToken);
    }

    public async Task<ProportionalElectionResultBallot> GetBallot(Guid bundleId, int ballotNumber)
    {
        var ballot = await _ballotRepo.Query()
                         .AsSplitQuery()
                         .Include(x => x.Bundle.ElectionResult.ProportionalElection.Translations)
                         .Include(x => x.BallotCandidates).ThenInclude(x => x.Candidate.Translations)
                         .Include(x => x.BallotCandidates).ThenInclude(x => x.Candidate.ProportionalElectionList.Translations)
                         .FirstOrDefaultAsync(x => x.BundleId == bundleId && x.Number == ballotNumber)
                     ?? throw new EntityNotFoundException(new { bundleId, ballotNumber });

        await EnsureCanReadBallot(ballot);

        ballot.BallotCandidates = ballot.BallotCandidates
            .OrderBy(c => c.Position)
            .ThenBy(c => c.RemovedFromList)
            .ToList();

        return ballot;
    }

    private async Task EnsureCanReadBallot(ProportionalElectionResultBallot ballot)
    {
        // The current tenant must have read permission on the counting circle where the ballot result comes from
        await _permissionService.EnsureCanReadCountingCircle(
            ballot.Bundle.ElectionResult.CountingCircleId,
            ballot.Bundle.ElectionResult.ProportionalElection.ContestId);

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
