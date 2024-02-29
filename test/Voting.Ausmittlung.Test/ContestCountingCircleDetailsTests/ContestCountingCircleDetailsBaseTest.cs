// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.ContestCountingCircleDetailsTests;

public abstract class ContestCountingCircleDetailsBaseTest : BaseTest<ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceClient>
{
    protected ContestCountingCircleDetailsBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceClient BundErfassungElectionAdminClient { get; private set; } = null!; // initialized during InitializeAsync

    public override async Task InitializeAsync()
    {
        BundErfassungElectionAdminClient = new ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, TestDefaults.UserId, RolesMockedData.ErfassungElectionAdmin));
        await base.InitializeAsync();
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected async Task<ContestDetails?> LoadContestDetails(Guid contestId)
    {
        var contestDetails = await RunOnDb(db => db
            .ContestDetails
            .AsSplitQuery()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .FirstOrDefaultAsync(cd => cd.ContestId == contestId));

        if (contestDetails == null)
        {
            return null;
        }

        // ensure consistent json snapshot
        foreach (var votingCardResultDetail in contestDetails!.VotingCards)
        {
            votingCardResultDetail.Id = Guid.Empty;
            votingCardResultDetail.ContestDetailsId = Guid.Empty;
        }

        foreach (var subtotal in contestDetails.CountOfVotersInformationSubTotals)
        {
            subtotal.Id = Guid.Empty;
            subtotal.ContestDetailsId = Guid.Empty;
        }

        contestDetails.OrderVotingCardsAndSubTotals();
        contestDetails.Id = Guid.Empty;
        return contestDetails;
    }

    protected async Task<List<ContestDomainOfInfluenceDetails>> LoadContestDomainOfInfluenceDetails(Guid contestId)
    {
        var contest = await RunOnDb(db => db.Contests.Include(x => x.DomainOfInfluence).FirstOrDefaultAsync(x => x.Id == contestId));
        var domainOfInfluenceDetails = await RunOnDb(db => db
            .ContestDomainOfInfluenceDetails
            .AsSplitQuery()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.DomainOfInfluence.Name)
            .Where(x => x.ContestId == contestId)
            .ToListAsync());

        if (domainOfInfluenceDetails.Count == 0)
        {
            return domainOfInfluenceDetails;
        }

        foreach (var doiDetail in domainOfInfluenceDetails)
        {
            // ensure consistent json snapshot
            foreach (var votingCardResultDetail in doiDetail!.VotingCards)
            {
                votingCardResultDetail.Id = Guid.Empty;
                votingCardResultDetail.ContestDomainOfInfluenceDetailsId = Guid.Empty;
            }

            foreach (var subtotal in doiDetail.CountOfVotersInformationSubTotals)
            {
                subtotal.Id = Guid.Empty;
                subtotal.ContestDomainOfInfluenceDetailsId = Guid.Empty;
            }

            doiDetail.OrderVotingCardsAndSubTotals();
            doiDetail.Id = Guid.Empty;
        }

        return domainOfInfluenceDetails;
    }
}
