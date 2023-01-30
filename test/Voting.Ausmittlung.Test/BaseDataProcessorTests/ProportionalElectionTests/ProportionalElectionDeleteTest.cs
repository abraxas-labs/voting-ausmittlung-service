// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
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
    public async Task TestDeleteRelatedVotingCards()
    {
        await TestEventPublisher.Publish(0, new ProportionalElectionDeleted
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
        });

        var details = await RunOnDb(db => db.ContestCountingCircleDetails
            .Include(x => x.VotingCards)
            .FirstOrDefaultAsync(c =>
                c.Id == ContestCountingCircleDetailsMockData.GossauUrnengangStGallen.Id));

        var contestDetails = await RunOnDb(db => db.ContestDetails
            .Include(x => x.VotingCards)
            .SingleAsync(c => c.Id == ContestDetailsMockedData.UrnengangStGallenDetails.Id));
        contestDetails.OrderVotingCardsAndSubTotals();
        var contestRelatedCountOfReceivedVotingCards = contestDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();

        var doiDetails = await RunOnDb(db => db.ContestDomainOfInfluenceDetails
            .Include(x => x.VotingCards)
            .SingleAsync(c => c.Id == ContestDomainOfInfluenceDetailsMockedData.StGallenUrnengangStGallenContestDomainOfInfluenceDetails.Id));
        doiDetails.OrderVotingCardsAndSubTotals();
        var doiRelatedCountOfReceivedVotingCards = doiDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();

        details!.VotingCards.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).Should().NotBeEmpty();
        contestRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 2000, 1000, 3000 }).Should().BeTrue();
        contestDetails.VotingCards.Any(x => x.CountOfReceivedVotingCards != 0).Should().BeTrue();
        doiRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 2000, 1000, 3000 }).Should().BeTrue();
        doiDetails.VotingCards.Any(x => x.CountOfReceivedVotingCards != 0).Should().BeTrue();

        await TestEventPublisher.Publish(1, new ProportionalElectionDeleted
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
        });

        details = await RunOnDb(db => db.ContestCountingCircleDetails
            .Include(x => x.VotingCards)
            .FirstOrDefaultAsync(c =>
                c.Id == ContestCountingCircleDetailsMockData.GossauUrnengangStGallen.Id));

        contestDetails = await RunOnDb(db => db.ContestDetails
            .Include(x => x.VotingCards)
            .SingleAsync(c => c.Id == ContestDetailsMockedData.UrnengangStGallenDetails.Id));
        contestDetails.OrderVotingCardsAndSubTotals();
        contestRelatedCountOfReceivedVotingCards = contestDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();

        doiDetails = await RunOnDb(db => db.ContestDomainOfInfluenceDetails
            .Include(x => x.VotingCards)
            .SingleAsync(c => c.Id == ContestDomainOfInfluenceDetailsMockedData.StGallenUrnengangStGallenContestDomainOfInfluenceDetails.Id));
        doiDetails.OrderVotingCardsAndSubTotals();
        doiRelatedCountOfReceivedVotingCards = doiDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();

        details!.VotingCards.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).Should().BeEmpty();
        contestRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 0, 0, 0 }).Should().BeTrue();
        doiRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 0, 0, 0 }).Should().BeTrue();
    }
}
