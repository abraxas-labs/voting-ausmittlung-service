// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Core.Services.Write;

public abstract class PoliticalBusinessResultBundleWriter<TResult, TBundleAggregate> : PoliticalBusinessResultWriter<TResult>
    where TResult : CountingCircleResult
    where TBundleAggregate : PoliticalBusinessResultBundleAggregate
{
    private readonly PermissionService _permissionService;

    protected PoliticalBusinessResultBundleWriter(
        PermissionService permissionService,
        ContestService contestService,
        IAuth auth,
        IAggregateRepository aggregateRepository,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
        : base(permissionService, contestService, auth, aggregateRepository, secondFactorTransactionWriter)
    {
        _permissionService = permissionService;
    }

    protected async Task<Guid> EnsureEditPermissionsForBundle(TBundleAggregate bundleAggregate)
    {
        var result = await LoadPoliticalBusinessResult(bundleAggregate.PoliticalBusinessResultId);
        return await EnsureEditPermissionsForBundle(result, bundleAggregate);
    }

    protected async Task<Guid> EnsureEditPermissionsForBundle(TResult result, TBundleAggregate bundleAggregate)
    {
        var contestId = await EnsurePoliticalBusinessPermissions(result);

        await EnsureBundleExists(bundleAggregate.Id);

        if (Auth.HasPermission(Permissions.PoliticalBusinessResultBundle.UpdateAll))
        {
            return contestId;
        }

        if (bundleAggregate.State != BallotBundleState.ReadyForReview && _permissionService.UserId != bundleAggregate.CreatedBy)
        {
            throw new ForbiddenException("only election admins or the creator of a bundle can edit it while not in review.");
        }

        return contestId;
    }

    protected async Task<Guid> EnsureReviewPermissionsForBundle(TBundleAggregate bundle, bool succeed)
    {
        var result = await LoadPoliticalBusinessResult(bundle.PoliticalBusinessResultId);
        var contestId = await EnsurePoliticalBusinessPermissions(result);

        if (bundle.CreatedBy == _permissionService.UserId)
        {
            throw new ForbiddenException("The creator of a bundle can't review it.");
        }

        await EnsureBundleExists(bundle.Id);

        if (succeed
            && !Auth.HasPermission(Permissions.PoliticalBusinessResultBundle.ReviewSelfModifiedBundle)
            && bundle.ModificationUsers.Contains(_permissionService.UserId))
        {
            throw new ReviewModifiedBundleForbiddenException();
        }

        return contestId;
    }

    protected abstract Task<bool> DoesBundleExist(Guid id);

    private async Task EnsureBundleExists(Guid id)
    {
        // A bundle may not exist in the read model, if someone triggered a "*ResultEntryDefined"
        // event (which deletes all bundles in the read model, but the aggregates still exist),
        // between a bundle create and a ballot create event.
        // That is why we do an additional check whether the bundle still exists here.
        if (!await DoesBundleExist(id))
        {
            throw new ValidationException($"Bundle with id {id} not found, it may have been deleted in the meantime");
        }
    }
}
