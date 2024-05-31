// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.Services.Read;

public class VoteResultBundleReader
{
    private readonly IDbRepository<DataContext, BallotResult> _resultRepo;
    private readonly IDbRepository<DataContext, VoteResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, VoteResultBallot> _ballotRepo;
    private readonly MessageConsumerHub<VoteBundleChanged, VoteResultBundle> _bundleChangeListener;
    private readonly PermissionService _permissionService;
    private readonly IAuth _auth;

    public VoteResultBundleReader(
        IDbRepository<DataContext, BallotResult> resultRepo,
        IDbRepository<DataContext, VoteResultBundle> bundleRepo,
        IDbRepository<DataContext, VoteResultBallot> ballotRepo,
        PermissionService permissionService,
        IAuth auth,
        MessageConsumerHub<VoteBundleChanged, VoteResultBundle> bundleChangeListener)
    {
        _resultRepo = resultRepo;
        _bundleRepo = bundleRepo;
        _ballotRepo = ballotRepo;
        _permissionService = permissionService;
        _auth = auth;
        _bundleChangeListener = bundleChangeListener;
    }

    public async Task<BallotResult> GetBallotResultWithBundles(Guid ballotResultId)
    {
        var ballotResult = await _resultRepo.Query()
                               .AsSplitQuery()
                               .Include(x => x.VoteResult.CountingCircle)
                               .Include(x => x.VoteResult.Vote.Translations)
                               .Include(x => x.VoteResult.Vote.DomainOfInfluence)
                               .Include(x => x.VoteResult.Vote.Contest.Translations)
                               .Include(x => x.VoteResult.Vote.Contest.CantonDefaults)
                               .Include(x => x.Bundles.OrderBy(b => b.Number))
                               .FirstOrDefaultAsync(x => x.Id == ballotResultId)
                           ?? throw new EntityNotFoundException(ballotResultId);

        await _permissionService.EnsureCanReadCountingCircle(ballotResult.VoteResult.CountingCircleId, ballotResult.VoteResult.Vote.ContestId);

        return ballotResult;
    }

    public async Task ListenToBundleChanges(
        Guid ballotResultId,
        Func<VoteResultBundle, Task> listener,
        CancellationToken cancellationToken)
    {
        var data = await _resultRepo.Query()
                       .Where(x => x.Id == ballotResultId)
                       .Select(x => new { x.VoteResult.CountingCircleId, x.VoteResult.Vote.ContestId })
                       .FirstOrDefaultAsync(cancellationToken)
                   ?? throw new EntityNotFoundException(ballotResultId);

        await _permissionService.EnsureCanReadCountingCircle(data.CountingCircleId, data.ContestId);

        await _bundleChangeListener.Listen(
            b => b.BallotResultId == ballotResultId,
            listener,
            cancellationToken);
    }

    public async Task<VoteResultBundle> GetBundle(Guid bundleId)
    {
        var bundle = await _bundleRepo.Query()
                         .AsSplitQuery()
                         .Include(x => x.BallotResult.VoteResult.CountingCircle)
                         .Include(x => x.BallotResult.VoteResult.Vote.Translations)
                         .Include(x => x.BallotResult.VoteResult.Vote.Contest.Translations)
                         .Include(x => x.BallotResult.VoteResult.Vote.Contest.CantonDefaults)
                         .Include(x => x.BallotResult.VoteResult.Vote.DomainOfInfluence)
                         .Include(x => x.BallotResult.VoteResult.Vote.Contest.DomainOfInfluence)
                         .Include(x => x.BallotResult.Ballot).ThenInclude(x => x.BallotQuestions).ThenInclude(x => x.Translations)
                         .Include(x => x.BallotResult.Ballot).ThenInclude(x => x.TieBreakQuestions).ThenInclude(x => x.Translations)
                         .FirstOrDefaultAsync(x => x.Id == bundleId)
                     ?? throw new EntityNotFoundException(bundleId);

        await _permissionService.EnsureCanReadCountingCircle(bundle.BallotResult.VoteResult.CountingCircleId, bundle.BallotResult.VoteResult.Vote.ContestId);

        bundle.BallotNumbersToReview = await _ballotRepo.Query()
            .Where(b => b.BundleId == bundle.Id && b.MarkedForReview)
            .Select(b => b.Number)
            .OrderBy(b => b)
            .ToListAsync();

        bundle.BallotResult.Ballot.OrderQuestions();
        return bundle;
    }

    public async Task<VoteResultBallot> GetBallot(Guid bundleId, int ballotNumber)
    {
        var ballot = await _ballotRepo.Query()
                         .AsSplitQuery()
                         .Include(x => x.Bundle.BallotResult.VoteResult.Vote)
                         .Include(x => x.QuestionAnswers).ThenInclude(x => x.Question.Translations)
                         .Include(x => x.TieBreakQuestionAnswers).ThenInclude(x => x.Question.Translations)
                         .FirstOrDefaultAsync(x => x.BundleId == bundleId && x.Number == ballotNumber)
                     ?? throw new EntityNotFoundException(new { bundleId, ballotNumber });

        await EnsureCanReadBallot(ballot);

        ballot.QuestionAnswers = ballot.QuestionAnswers
            .OrderBy(b => b.Question.Number)
            .ToList();

        ballot.TieBreakQuestionAnswers = ballot.TieBreakQuestionAnswers
            .OrderBy(b => b.Question.Number)
            .ToList();

        return ballot;
    }

    private async Task EnsureCanReadBallot(VoteResultBallot ballot)
    {
        // The current tenant must have read permission on the counting circle where the ballot result comes from
        await _permissionService.EnsureCanReadCountingCircle(
            ballot.Bundle.BallotResult.VoteResult.CountingCircleId,
            ballot.Bundle.BallotResult.VoteResult.Vote.ContestId);

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
