// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReferenceCreateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCandidateReferenceCreateTest(TestApplicationFactory factory)
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
    public async Task TestCreateCandidateReference()
    {
        var id = Guid.Parse("2e2d586f-dc6d-45ce-a0b8-39a70d9cf46a");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceCreated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = id.ToString(),
                    SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    Position = 3,
                    Incumbent = false,
                    Number = "1.2",
                    CheckDigit = 4,
                    CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                    ReportingType = SharedProto.MajorityElectionCandidateReportingType.Candidate,
                },
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == id),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestCreateCandidateReferenceOnSeparateBallot()
    {
        var id = Guid.Parse("4b585762-c6df-4d2e-ad82-d645cb421cec");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceCreated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = id.ToString(),
                    IsOnSeparateBallot = true,
                    SecondaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot,
                    Position = 3,
                    Incumbent = false,
                    Number = "1.2",
                    CheckDigit = 4,
                    CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                },
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == id),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestCreateCandidateReferenceAfterSubmissionStarted()
    {
        await MajorityElectionEndResultMockedData.Seed(RunScoped);

        var detailedResultId = MajorityElectionEndResultMockedData.StGallenResultGuid;

        await ModifyDbEntities<MajorityElectionResult>(
            r => r.Id == detailedResultId,
            r => r.Entry = MajorityElectionResultEntry.Detailed);

        var id = Guid.Parse("4c03f2b6-adde-42d2-a762-d0c065892394");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceCreated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = id.ToString(),
                    SecondaryMajorityElectionId = MajorityElectionEndResultMockedData.SecondaryElectionId,
                    Position = 5,
                    Incumbent = false,
                    Number = "1.2",
                    CheckDigit = 4,
                    CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                },
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .AsSplitQuery()
                .Where(x => x.Id == id)
                .Include(x => x.CandidateResults).ThenInclude(x => x.ElectionResult)
                .Include(x => x.EndResult)
                .Include(x => x.Translations)
                .FirstAsync(),
            Languages.German);

        candidate.CandidateResults.Where(c => c.ElectionResult.PrimaryResultId == detailedResultId).All(c => c.ConventionalVoteCount == 0).Should().BeTrue();
        candidate.CandidateResults.Where(c => c.ElectionResult.PrimaryResultId != detailedResultId).All(c => c.ConventionalVoteCount == null).Should().BeTrue();
        candidate.CandidateResults = null!;

        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot(c => c.EndResult!.Id, c => c.EndResult!.SecondaryMajorityElectionEndResultId);
    }

    [Fact]
    public async Task TestCreateCandidateReferenceAfterSubmissionStartedOnSeparateBallot()
    {
        await MajorityElectionEndResultMockedData.Seed(RunScoped, secondaryOnSeparateBallot: true);

        var id = Guid.Parse("6a63829a-660d-46ac-bba7-9107334f1951");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceCreated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = id.ToString(),
                    SecondaryMajorityElectionId = MajorityElectionEndResultMockedData.SecondaryElectionId,
                    Position = 5,
                    Incumbent = false,
                    Number = "1.2",
                    CheckDigit = 4,
                    CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                    IsOnSeparateBallot = true,
                },
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Where(x => x.Id == id)
                .Include(x => x.EndResult)
                .Include(x => x.Translations)
                .FirstAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot(c => c.EndResult!.Id, c => c.EndResult!.MajorityElectionEndResultId);
    }
}
