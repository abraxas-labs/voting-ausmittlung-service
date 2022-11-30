// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ContestCountingCircleDetailsTests;

public class ContestCountingCircleDetailsResetTest : BaseProcessorTest
{
    private static readonly Guid ContestCountingCircleDetailsId = ContestCountingCircleDetailsMockData.GuidGossauUrnengangBundContestCountingCircleDetails;
    private static readonly Guid ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
    private static readonly Guid CountingCircleId = CountingCircleMockedData.GuidGossau;
    private static readonly Guid ContestDomainOfInfluenceDetailsId = Guid.Parse(ContestDomainOfInfluenceDetailsMockedData.IdBundUrnengangGossauContestDomainOfInfluenceDetails);

    public ContestCountingCircleDetailsResetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await base.InitializeAsync();
    }

    [Fact]
    public async Task TestReset()
    {
        var ccId = CountingCircleMockedData.GuidGossau;
        var ccResultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestBund;

        // to test that ContestCountingCircleDetailsNotUpdatableException is not throwed.
        await ModifyDbEntities<ProportionalElectionResult>(
            r => r.Id == ccResultId,
            r => r.State = CountingCircleResultState.SubmissionDone);

        var ccDetails = await LoadCountingCircleDetails();
        ccDetails.TotalCountOfVoters.Should().Be(15800);
        ccDetails.MatchSnapshot("ccDetailsBefore");

        var results = await RunOnDb(db => db.ProportionalElectionResults
            .Where(r => r.CountingCircle.BasisCountingCircleId == CountingCircleId && r.ProportionalElection.ContestId == ContestId)
            .ToListAsync());
        results.Any().Should().BeTrue();
        results.Any(r => r.TotalCountOfVoters != 0).Should().BeTrue();

        await TestEventPublisher.Publish(new ContestCountingCircleDetailsResetted
        {
            EventInfo = GetMockedEventInfo(),
            Id = ContestCountingCircleDetailsId.ToString(),
            ContestId = ContestId.ToString(),
            CountingCircleId = ccId.ToString(),
        });

        ccDetails = await LoadCountingCircleDetails();
        ccDetails.TotalCountOfVoters.Should().Be(0);
        ccDetails.MatchSnapshot("ccDetailsAfter");

        results = await RunOnDb(db => db.ProportionalElectionResults
            .Where(r => r.CountingCircle.BasisCountingCircleId == CountingCircleId && r.ProportionalElection.ContestId == ContestId)
            .ToListAsync());
        results.Any().Should().BeTrue();
        results.All(r => r.TotalCountOfVoters == 0).Should().BeTrue();
    }

    [Fact]
    public async Task TestResetShouldResetAggregatedDetails()
    {
        var doiDetails = await LoadContestDomainOfInfluenceDetails();
        doiDetails.TotalCountOfVoters.Should().Be(15800);
        doiDetails.MatchSnapshot("doiDetailsBefore");
        var contestDetails = await LoadContestDetails();
        contestDetails.TotalCountOfVoters.Should().Be(34810);
        contestDetails.MatchSnapshot("contestDetailsBefore");

        await TestEventPublisher.Publish(new ContestCountingCircleDetailsResetted
        {
            EventInfo = GetMockedEventInfo(),
            Id = ContestCountingCircleDetailsId.ToString(),
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
        });

        doiDetails = await LoadContestDomainOfInfluenceDetails();
        doiDetails.TotalCountOfVoters.Should().Be(0);
        doiDetails.MatchSnapshot("doiDetailsAfter");
        contestDetails = await LoadContestDetails();
        contestDetails.TotalCountOfVoters.Should().Be(19010);
        contestDetails.MatchSnapshot("contestDetailsAfter");
    }

    private async Task<ContestCountingCircleDetails> LoadCountingCircleDetails()
    {
        var ccDetails = await RunOnDb(db => db
            .ContestCountingCircleDetails
            .AsSplitQuery()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .SingleAsync(cd => cd.Id == ContestCountingCircleDetailsId));

        // ensure consistent json snapshot
        foreach (var votingCardResultDetail in ccDetails!.VotingCards)
        {
            votingCardResultDetail.Id = Guid.Empty;
            votingCardResultDetail.ContestCountingCircleDetailsId = Guid.Empty;
        }

        foreach (var subtotal in ccDetails.CountOfVotersInformationSubTotals)
        {
            subtotal.Id = Guid.Empty;
            subtotal.ContestCountingCircleDetailsId = Guid.Empty;
        }

        ccDetails.OrderVotingCardsAndSubTotals();
        ccDetails.Id = Guid.Empty;
        return ccDetails;
    }

    private async Task<ContestDetails> LoadContestDetails()
    {
        var contestDetails = await RunOnDb(db => db
            .ContestDetails
            .AsSplitQuery()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .SingleAsync(cd => cd.ContestId == ContestId));

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

    private async Task<ContestDomainOfInfluenceDetails> LoadContestDomainOfInfluenceDetails()
    {
        var doiDetail = await RunOnDb(db => db
            .ContestDomainOfInfluenceDetails
            .AsSplitQuery()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.DomainOfInfluence.Name)
            .SingleAsync(x => x.Id == ContestDomainOfInfluenceDetailsId));

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

        return doiDetail;
    }
}
