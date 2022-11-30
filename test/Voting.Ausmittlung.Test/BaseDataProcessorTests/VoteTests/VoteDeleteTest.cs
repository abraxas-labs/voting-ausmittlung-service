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
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class VoteDeleteTest : VoteProcessorBaseTest
{
    public VoteDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestDeleted()
    {
        await TestEventPublisher.Publish(
            new VoteDeleted
            {
                VoteId = VoteMockedData.IdUzwilVoteInContestBund,
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdUzwilVoteInContestBund));
        data.Count.Should().Be(0);

        var simpleResult = await RunOnDb(db => db.SimplePoliticalBusinesses.FirstOrDefaultAsync(c =>
            c.Id == Guid.Parse(VoteMockedData.IdUzwilVoteInContestBund)));
        simpleResult.Should().BeNull();
    }

    [Fact]
    public async Task TestDeleteRelatedVotingCards()
    {
        await TestEventPublisher.Publish(0, new VoteDeleted
        {
            VoteId = VoteMockedData.IdStGallenVoteInContestBund,
        });

        var details = await RunOnDb(db => db.ContestCountingCircleDetails
            .Include(x => x.VotingCards)
            .FirstOrDefaultAsync(c =>
                c.Id == ContestCountingCircleDetailsMockData.UzwilUrnengangBund.Id));

        var contestDetails = await RunOnDb(db => db.ContestDetails
            .Include(x => x.VotingCards)
            .SingleAsync(c => c.Id == ContestDetailsMockedData.UrnengangBundContestDetails.Id));
        contestDetails.OrderVotingCardsAndSubTotals();
        var contestRelatedCountOfReceivedVotingCards = contestDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();

        var doiDetails = await RunOnDb(db => db.ContestDomainOfInfluenceDetails
            .Include(x => x.VotingCards)
            .SingleAsync(c => c.Id == ContestDomainOfInfluenceDetailsMockedData.BundUrnengangUzwilContestDomainOfInfluenceDetails.Id));
        doiDetails.OrderVotingCardsAndSubTotals();
        var doiRelatedCountOfReceivedVotingCards = doiDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();

        details!.VotingCards.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).Should().NotBeEmpty();
        contestRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 4000, 3400, 300, 4400, 270 }).Should().BeTrue();
        doiRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 2000, 150, 200, 400, 150 }).Should().BeTrue();

        await TestEventPublisher.Publish(1, new VoteDeleted
        {
            VoteId = VoteMockedData.IdGenfVoteInContestBundWithoutChilds,
        });

        details = await RunOnDb(db => db.ContestCountingCircleDetails
            .Include(x => x.VotingCards)
            .FirstOrDefaultAsync(c =>
                c.Id == ContestCountingCircleDetailsMockData.UzwilUrnengangBund.Id));

        contestDetails = await RunOnDb(db => db.ContestDetails
            .Include(x => x.VotingCards)
            .SingleAsync(c => c.Id == ContestDetailsMockedData.UrnengangBundContestDetails.Id));
        contestDetails.OrderVotingCardsAndSubTotals();
        contestRelatedCountOfReceivedVotingCards = contestDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();

        doiDetails = await RunOnDb(db => db.ContestDomainOfInfluenceDetails
            .Include(x => x.VotingCards)
            .SingleAsync(c => c.Id == ContestDomainOfInfluenceDetailsMockedData.BundUrnengangUzwilContestDomainOfInfluenceDetails.Id));
        doiDetails.OrderVotingCardsAndSubTotals();
        doiRelatedCountOfReceivedVotingCards = doiDetails.VotingCards
            .Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct)
            .Select(x => x.CountOfReceivedVotingCards)
            .ToList();

        details!.VotingCards.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).Should().BeEmpty();
        contestRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 0, 0, 0, 0, 0 }).Should().BeTrue();
        contestDetails.VotingCards.Any(x => x.CountOfReceivedVotingCards != 0).Should().BeTrue();
        doiRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 0, 0, 0, 0, 0 }).Should().BeTrue();
        doiDetails.VotingCards.Any(x => x.CountOfReceivedVotingCards != 0).Should().BeTrue();
    }
}
