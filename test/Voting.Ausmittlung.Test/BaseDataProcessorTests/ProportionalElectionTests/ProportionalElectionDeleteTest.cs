// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionDeleteTest : BaseDataProcessorTest
{
    public ProportionalElectionDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestDelete()
    {
        await TestEventPublisher.Publish(new ProportionalElectionDeleted
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
        });

        var item = await RunOnDb(db => db.ProportionalElections
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen)));
        item.Should().BeNull();
    }

    [Fact]
    public async Task TestDeleteAggregatedChanges()
    {
        var id = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen);
        await TestEventPublisher.Publish(new ProportionalElectionDeleted { ProportionalElectionId = id.ToString() });

        var unionLists = await RunOnDb(
            db => db.ProportionalElectionUnionLists
                .Include(ul => ul.Translations)
                .Where(l => Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1) == l.ProportionalElectionUnionId)
                .Select(l => new { l.OrderNumber, l.Translations })
                .OrderBy(x => x.OrderNumber)
                .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(unionLists.SelectMany(x => x.Translations));
        unionLists.MatchSnapshot();

        var simpleResult = await RunOnDb(db => db.SimplePoliticalBusinesses.FirstOrDefaultAsync(c =>
            c.Id == id));
        simpleResult.Should().BeNull();
    }

    [Fact]
    public async Task TestDeleteRelatedVotingCardsAndSubTotals()
    {
        await TestEventPublisher.Publish(0, new ProportionalElectionDeleted
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
        });

        var details = await RunOnDb(db => db.ContestCountingCircleDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(c =>
                c.Id == ContestCountingCircleDetailsMockData.GossauUrnengangStGallen.Id));

        var contestDetails = await RunOnDb(db => db.ContestDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .SingleAsync(c => c.Id == ContestDetailsMockedData.UrnengangStGallenDetails.Id));
        contestDetails.OrderVotingCardsAndSubTotals();
        var contestRelatedCountOfReceivedVotingCards = contestDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();
        var contestRelatedCountOfVoters = contestDetails.CountOfVotersInformationSubTotals
             .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
             .Select(x => x.CountOfVoters)
             .ToList();

        var doiDetails = await RunOnDb(db => db.ContestDomainOfInfluenceDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .SingleAsync(c => c.Id == ContestDomainOfInfluenceDetailsMockedData.StGallenUrnengangStGallenContestDomainOfInfluenceDetails.Id));
        doiDetails.OrderVotingCardsAndSubTotals();
        var doiRelatedCountOfReceivedVotingCards = doiDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();
        var doiRelatedCountOfVoters = doiDetails.CountOfVotersInformationSubTotals
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfVoters)
            .ToList();

        details!.VotingCards.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).Should().NotBeEmpty();
        details!.CountOfVotersInformationSubTotals.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).Should().NotBeEmpty();
        contestRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 2000, 1000, 3000 }).Should().BeTrue();
        doiRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 2000, 1000, 3000 }).Should().BeTrue();
        contestRelatedCountOfVoters.SequenceEqual(new[] { 8000, 500, 7000, 300 }).Should().BeTrue();
        doiRelatedCountOfVoters.SequenceEqual(new[] { 8000, 500, 7000, 300 }).Should().BeTrue();

        await ModifyDbEntities<SimpleCountingCircleResult>(
            x => x.PoliticalBusinessId == ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallenWithoutChilds.Id,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(1, new ProportionalElectionDeleted
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
        });

        details = await RunOnDb(db => db.ContestCountingCircleDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(c =>
                c.Id == ContestCountingCircleDetailsMockData.GossauUrnengangStGallen.Id));

        contestDetails = await RunOnDb(db => db.ContestDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .SingleAsync(c => c.Id == ContestDetailsMockedData.UrnengangStGallenDetails.Id));
        contestDetails.OrderVotingCardsAndSubTotals();
        contestRelatedCountOfReceivedVotingCards = contestDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();
        contestRelatedCountOfVoters = contestDetails.CountOfVotersInformationSubTotals
             .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
             .Select(x => x.CountOfVoters)
             .ToList();

        doiDetails = await RunOnDb(db => db.ContestDomainOfInfluenceDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .SingleAsync(c => c.Id == ContestDomainOfInfluenceDetailsMockedData.StGallenUrnengangStGallenContestDomainOfInfluenceDetails.Id));
        doiDetails.OrderVotingCardsAndSubTotals();
        doiRelatedCountOfReceivedVotingCards = doiDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();
        doiRelatedCountOfVoters = doiDetails.CountOfVotersInformationSubTotals
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfVoters)
            .ToList();

        details!.VotingCards.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).Should().BeEmpty();
        contestRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 0, 0, 0 }).Should().BeTrue();
        contestRelatedCountOfVoters.SequenceEqual(new[] { 0, 0, 0, 0 }).Should().BeTrue();
        contestDetails.VotingCards.Any(x => x.CountOfReceivedVotingCards != 0).Should().BeTrue();
        contestDetails.CountOfVotersInformationSubTotals.Any(x => x.CountOfVoters != 0).Should().BeTrue();
        doiRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 0, 0, 0 }).Should().BeTrue();
        doiRelatedCountOfVoters.SequenceEqual(new[] { 0, 0, 0, 0 }).Should().BeTrue();
        doiDetails.VotingCards.Any(x => x.CountOfReceivedVotingCards != 0).Should().BeTrue();
        doiDetails.CountOfVotersInformationSubTotals.Any(x => x.CountOfVoters != 0).Should().BeTrue();
    }

    [Fact]
    public async Task TestAdjustElectionsCountOnUnionEndResults()
    {
        await ZhMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new ProportionalElectionDeleted
            {
                ProportionalElectionId = ZhMockedData.ProportionalElectionGuidKtratWinterthur.ToString(),
            });

        var unionEndResult = await RunOnDb(db => db.ProportionalElectionUnionEndResults
            .SingleAsync(c => c.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat));

        unionEndResult.TotalCountOfElections.Should().Be(2);
        unionEndResult.CountOfDoneElections.Should().Be(2);
    }

    [Fact]
    public async Task TestDeleteRelatedProtocolExports()
    {
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProtocolExportStarted
        {
            ProtocolExportId = Guid.NewGuid().ToString(),
            PoliticalBusinessId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            PoliticalBusinessIds = { ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen },
            ContestId = ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallen.ContestId.ToString(),
            EventInfo = new EventInfo
            {
                Timestamp = new Timestamp
                {
                    Seconds = 1594980476,
                },
                Tenant = new EventInfoTenant
                {
                    Id = SecureConnectTestDefaults.MockedTenantDefault.Id,
                    Name = SecureConnectTestDefaults.MockedTenantDefault.Name,
                },
                User = new EventInfoUser
                {
                    Id = SecureConnectTestDefaults.MockedUserDefault.Loginid,
                    FirstName = SecureConnectTestDefaults.MockedUserDefault.Firstname ?? string.Empty,
                    LastName = SecureConnectTestDefaults.MockedUserDefault.Lastname ?? string.Empty,
                    Username = SecureConnectTestDefaults.MockedUserDefault.Username,
                },
            },
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionDeleted
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
        });

        var protocolExists = await RunOnDb(db => db.ProtocolExports
            .AnyAsync(c => c.PoliticalBusinessId == ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallen.Id));
        protocolExists.Should().BeFalse();
    }
}
