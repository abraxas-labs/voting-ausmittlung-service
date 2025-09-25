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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionTests;

public class MajorityElectionDeleteTest : BaseDataProcessorTest
{
    public MajorityElectionDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestDelete()
    {
        await TestEventPublisher.Publish(new MajorityElectionDeleted
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
        });

        var result = await RunOnDb(db => db.MajorityElections.FirstOrDefaultAsync(c =>
            c.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)));
        result.Should().BeNull();

        var simpleResult = await RunOnDb(db => db.SimplePoliticalBusinesses.FirstOrDefaultAsync(c =>
            c.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)));
        simpleResult.Should().BeNull();
    }

    [Fact]
    public async Task TestDeleteRelatedVotingCardsAndSubTotals()
    {
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionDeleted
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot,
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionDeleted
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
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

        var electionId = Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenWithoutChilds);
        await ModifyDbEntities<SimpleCountingCircleResult>(
            x => x.PoliticalBusinessId == electionId,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionDeleted
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenWithoutChilds,
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
    public async Task TestDeleteRelatedProtocolExports()
    {
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProtocolExportStarted
        {
            ProtocolExportId = Guid.NewGuid().ToString(),
            PoliticalBusinessId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            PoliticalBusinessIds = { MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen },
            ContestId = MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.ContestId.ToString(),
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
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionDeleted
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
        });

        var protocolExists = await RunOnDb(db => db.ProtocolExports
            .AnyAsync(c => c.PoliticalBusinessId == MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.Id));
        protocolExists.Should().BeFalse();
    }
}
