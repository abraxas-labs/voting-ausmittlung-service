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
using Voting.Lib.Iam.Testing.AuthenticationScheme;
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
                VoteId = VoteMockedData.IdUzwilVoteInContestBundWithoutChilds,
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdUzwilVoteInContestBundWithoutChilds));
        data.Count.Should().Be(0);

        var simpleResult = await RunOnDb(db => db.SimplePoliticalBusinesses.FirstOrDefaultAsync(c =>
            c.Id == Guid.Parse(VoteMockedData.IdUzwilVoteInContestBundWithoutChilds)));
        simpleResult.Should().BeNull();
    }

    [Fact]
    public async Task TestDeleteRelatedVotingCardsAndSubTotals()
    {
        await TestEventPublisher.Publish(0, new VoteDeleted
        {
            VoteId = VoteMockedData.IdStGallenVoteInContestBund,
        });

        var details = await RunOnDb(db => db.ContestCountingCircleDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(c =>
                c.Id == ContestCountingCircleDetailsMockData.UzwilUrnengangBund.Id));

        var contestDetails = await RunOnDb(db => db.ContestDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .SingleAsync(c => c.Id == ContestDetailsMockedData.UrnengangBundContestDetails.Id));
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
            .SingleAsync(c => c.Id == ContestDomainOfInfluenceDetailsMockedData.BundUrnengangUzwilContestDomainOfInfluenceDetails.Id));
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
        contestRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 4400, 300, 4000, 3400, 270 }).Should().BeTrue();
        doiRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 400, 200, 2000, 150, 150 }).Should().BeTrue();
        contestRelatedCountOfVoters.SequenceEqual(new[] { 17600, 1100, 15400, 710 }).Should().BeTrue();
        doiRelatedCountOfVoters.SequenceEqual(new[] { 1600, 100, 1400, 110 }).Should().BeTrue();

        await ModifyDbEntities<SimpleCountingCircleResult>(
            _ => true,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(1, new VoteDeleted
        {
            VoteId = VoteMockedData.IdGenfVoteInContestBundWithoutChilds,
        });

        details = await RunOnDb(db => db.ContestCountingCircleDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(c =>
                c.Id == ContestCountingCircleDetailsMockData.UzwilUrnengangBund.Id));

        contestDetails = await RunOnDb(db => db.ContestDetails
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .SingleAsync(c => c.Id == ContestDetailsMockedData.UrnengangBundContestDetails.Id));
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
            .SingleAsync(c => c.Id == ContestDomainOfInfluenceDetailsMockedData.BundUrnengangUzwilContestDomainOfInfluenceDetails.Id));
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
        contestRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 0, 0, 0, 0, 0 }).Should().BeTrue();
        contestRelatedCountOfVoters.SequenceEqual(new[] { 0, 0, 0, 0 }).Should().BeTrue();
        contestDetails.VotingCards.Any(x => x.CountOfReceivedVotingCards != 0).Should().BeTrue();
        contestDetails.CountOfVotersInformationSubTotals.Any(x => x.CountOfVoters != 0).Should().BeTrue();
        doiRelatedCountOfReceivedVotingCards.SequenceEqual(new[] { 0, 0, 0, 0, 0 }).Should().BeTrue();
        doiRelatedCountOfVoters.SequenceEqual(new[] { 0, 0, 0, 0 }).Should().BeTrue();
        doiDetails.VotingCards.Any(x => x.CountOfReceivedVotingCards != 0).Should().BeTrue();
        doiDetails.CountOfVotersInformationSubTotals.Any(x => x.CountOfVoters != 0).Should().BeTrue();
    }

    [Fact]
    public async Task TestDeleteRelatedProtocolExports()
    {
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProtocolExportStarted
        {
            ProtocolExportId = Guid.NewGuid().ToString(),
            PoliticalBusinessId = VoteMockedData.IdStGallenVoteInContestBund,
            PoliticalBusinessIds = { VoteMockedData.IdStGallenVoteInContestBund },
            ContestId = VoteMockedData.StGallenVoteInContestBund.ContestId.ToString(),
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
        await TestEventPublisher.Publish(GetNextEventNumber(), new VoteDeleted
        {
            VoteId = VoteMockedData.IdStGallenVoteInContestBund,
        });

        var protocolExists = await RunOnDb(db => db.ProtocolExports
            .AnyAsync(c => c.PoliticalBusinessId == VoteMockedData.StGallenVoteInContestBund.Id));
        protocolExists.Should().BeFalse();
    }
}
