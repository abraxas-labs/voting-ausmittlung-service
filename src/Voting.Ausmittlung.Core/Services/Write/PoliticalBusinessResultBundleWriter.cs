// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Exceptions;

namespace Voting.Ausmittlung.Core.Services.Write;

public abstract class PoliticalBusinessResultBundleWriter<TResult, TBundleAggregate> : PoliticalBusinessResultWriter<TResult>
    where TResult : CountingCircleResult
    where TBundleAggregate : PoliticalBusinessResultBundleAggregate
{
    private readonly PermissionService _permissionService;

    protected PoliticalBusinessResultBundleWriter(
        PermissionService permissionService,
        ContestService contestService,
        IAggregateRepository aggregateRepository)
        : base(permissionService, contestService, aggregateRepository)
    {
        _permissionService = permissionService;
    }

    protected async Task<Guid> EnsureEditPermissionsForBundle(TBundleAggregate bundleAggregate, bool requireElectionAdmin)
    {
        var result = await LoadPoliticalBusinessResult(bundleAggregate.PoliticalBusinessResultId);
        return await EnsureEditPermissionsForBundle(result, bundleAggregate, requireElectionAdmin);
    }

    protected async Task<Guid> EnsureEditPermissionsForBundle(TResult result, TBundleAggregate bundleAggregate, bool requireElectionAdmin)
    {
        var contestId = await EnsurePoliticalBusinessPermissions(result, requireElectionAdmin);
        if (_permissionService.IsErfassungElectionAdmin())
        {
            return contestId;
        }

        if (requireElectionAdmin)
        {
            throw new ForbiddenException("election admin rights are required for this operation");
        }

        if (bundleAggregate.State == BallotBundleState.ReadyForReview)
        {
            if (_permissionService.UserId == bundleAggregate.CreatedBy)
            {
                throw new ForbiddenException("the creator of a bundle can't edit it while it is under review");
            }

            return contestId;
        }

        if (_permissionService.UserId != bundleAggregate.CreatedBy)
        {
            throw new ForbiddenException("only election admins or the creator of a bundle can edit it.");
        }

        return contestId;
    }

    protected async Task<Guid> EnsureReviewPermissionsForBundle(TBundleAggregate bundle)
    {
        var result = await LoadPoliticalBusinessResult(bundle.PoliticalBusinessResultId);
        var contestId = await EnsurePoliticalBusinessPermissions(result, false);

        if (bundle.CreatedBy == _permissionService.UserId)
        {
            throw new ForbiddenException("The creator of a bundle can't review it.");
        }

        return contestId;
    }
}
