// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ContestCountingCircleDetailsTests;

public class ContestCountingCircleDetailsResetTest : BaseIntegrationTest
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
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var ccId = CountingCircleMockedData.GuidGossau;
        var ccResultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestBund;

        // to test that ContestCountingCircleDetailsNotUpdatableException is not throwed.
        await ModifyDbEntities<ProportionalElectionResult>(
            r => r.Id == ccResultId,
            r => r.State = CountingCircleResultState.SubmissionDone);

        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.Id == ContestCountingCircleDetailsId,
            x => x.CountingMachine = CountingMachine.CalibratedScales);

        await RunOnDb(async db =>
        {
            var snapshotCountingCircle = await db.CountingCircles
                .SingleAsync(cc => cc.BasisCountingCircleId == ccId && cc.SnapshotContestId == contestId);

            db.ProtocolExports.Add(new()
            {
                CountingCircleId = snapshotCountingCircle.Id,
                ContestId = contestId,
                ExportTemplateId = Guid.Parse("f70ae784-2c18-4337-802a-934f04cf1ccb"),
                Started = new(2020, 1, 15, 20, 0, 0, DateTimeKind.Utc),
                State = ProtocolExportState.Completed,
            });
            await db.SaveChangesAsync();
        });

        var ccDetails = await LoadCountingCircleDetails();
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
        ccDetails.CountingMachine.Should().Be(CountingMachine.Unspecified);
        ccDetails.MatchSnapshot("ccDetailsAfter");

        results = await RunOnDb(db => db.ProportionalElectionResults
            .Where(r => r.CountingCircle.BasisCountingCircleId == CountingCircleId && r.ProportionalElection.ContestId == ContestId)
            .ToListAsync());
        results.Any().Should().BeTrue();
        results.All(r => r.TotalCountOfVoters == 0).Should().BeTrue();

        var protocolExports = await RunOnDb(db => db.ProtocolExports
            .Where(e => e.CountingCircle!.BasisCountingCircleId == ccId && e.ContestId == contestId)
            .ToListAsync());
        protocolExports.Should().HaveCount(0);
    }

    [Fact]
    public async Task TestResetShouldResetAggregatedDetails()
    {
        var doiDetails = await LoadContestDomainOfInfluenceDetails();
        doiDetails.MatchSnapshot("doiDetailsBefore");
        var contestDetails = await LoadContestDetails();
        contestDetails.MatchSnapshot("contestDetailsBefore");

        // Only aggregated details with a result in submission done will be reset
        await ModifyDbEntities<ProportionalElectionResult>(
            r => r.Id == CountingCircleId,
            r => r.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(new ContestCountingCircleDetailsResetted
        {
            EventInfo = GetMockedEventInfo(),
            Id = ContestCountingCircleDetailsId.ToString(),
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
        });

        doiDetails = await LoadContestDomainOfInfluenceDetails();
        doiDetails.MatchSnapshot("doiDetailsAfter");
        contestDetails = await LoadContestDetails();
        contestDetails.MatchSnapshot("contestDetailsAfter");
    }

    [Fact]
    public async Task TestResetShouldResetEndResultsWithSubmissionDone()
    {
        var ccResultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestBund;

        // End result will only be resetted, when an related counting circle result is in submission done.
        await ModifyDbEntities<ProportionalElectionResult>(
            r => r.Id == ccResultId,
            r => r.State = CountingCircleResultState.SubmissionDone);

        await ModifyDbEntities<SimpleCountingCircleResult>(
            r => r.Id == ccResultId,
            r => r.State = CountingCircleResultState.SubmissionDone);

        await ModifyDbEntities<ProportionalElectionListResult>(
            r => r.ResultId == ccResultId,
            r => r.ConventionalSubTotal.UnmodifiedListsCount = 10);

        await ModifyDbEntities<ProportionalElectionCandidateResult>(
            r => r.ListResult.ResultId == ccResultId,
            r => r.ConventionalSubTotal.UnmodifiedListVotesCount = 10);

        await RunScoped<ProportionalElectionEndResultBuilder>(async b => await b.AdjustEndResult(ccResultId, false));
        var endResultBefore = await LoadProportionalElectionEndResult();

        await TestEventPublisher.Publish(new ContestCountingCircleDetailsResetted
        {
            EventInfo = GetMockedEventInfo(),
            Id = ContestCountingCircleDetailsId.ToString(),
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
        });

        var endResultAfter = await LoadProportionalElectionEndResult();
        endResultBefore.MatchSnapshot("endResultBefore");
        endResultAfter.MatchSnapshot("endResultAfter");
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

    private async Task<ProportionalElectionEndResult> LoadProportionalElectionEndResult()
    {
        var endResult = await RunOnDb(
            db => db
                .ProportionalElectionEndResult
                .AsSplitQuery()
                .Include(r => r.VotingCards)
                .Include(r => r.CountOfVotersInformationSubTotals)
                .Include(r => r.ListEndResults.OrderBy(x => x.List.Position))
                    .ThenInclude(x => x.CandidateEndResults.OrderBy(x => x.Candidate.Position))
                .SingleAsync(r => r.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund)));

        SetDynamicIdToDefaultValue(endResult.VotingCards);
        SetDynamicIdToDefaultValue(endResult.CountOfVotersInformationSubTotals);
        SetDynamicIdToDefaultValue(endResult.ListEndResults);

        endResult.OrderVotingCardsAndSubTotals();

        foreach (var candidateEndResult in endResult.ListEndResults.SelectMany(x => x.CandidateEndResults))
        {
            candidateEndResult.Id = Guid.Empty;
            candidateEndResult.ListEndResultId = Guid.Empty;
        }

        return endResult;
    }
}
