// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionBallotGroupTests;

public class MajorityElectionBallotGroupCandidatesUpdateTest : BaseDataProcessorTest
{
    public MajorityElectionBallotGroupCandidatesUpdateTest(TestApplicationFactory factory)
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
    public async Task TestUpdateCandidate()
    {
        await TestEventPublisher.Publish(new MajorityElectionBallotGroupCandidatesUpdated
        {
            BallotGroupCandidates = new MajorityElectionBallotGroupCandidatesEventData
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                EntryCandidates =
                    {
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
                            CandidateIds =
                            {
                                MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                            },
                        },
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                            CandidateIds =
                            {
                                MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                            },
                        },
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId3StGallenMajorityElectionInContestBund,
                            IndividualCandidatesVoteCount = 1,
                        },
                    },
            },
        });

        var group = await RunOnDb(db => db.MajorityElectionBallotGroups
            .AsSplitQuery()
            .Include(x => x.Entries)
            .ThenInclude(x => x.Candidates)
            .FirstAsync(x =>
                x.Id == Guid.Parse(MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)));

        foreach (var entry in group.Entries)
        {
            entry.Id = Guid.Empty;

            foreach (var candidate in entry.Candidates)
            {
                candidate.Id = Guid.Empty;
            }
        }

        group.Entries = group.Entries
            .OrderBy(x => x.Candidates.Count == 0 ? null : x.Candidates.First().PrimaryElectionCandidateId)
            .ThenBy(x => x.IndividualCandidatesVoteCount)
            .ToList();

        group.AllCandidateCountsOk.Should().BeTrue();
        group.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdateCandidateShouldSetAllCandidatesCountOkFalse()
    {
        await TestEventPublisher.Publish(new MajorityElectionBallotGroupCandidatesUpdated
        {
            BallotGroupCandidates = new MajorityElectionBallotGroupCandidatesEventData
            {
                BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                EntryCandidates =
                    {
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId1StGallenMajorityElectionInContestBund,
                            CandidateIds =
                            {
                                MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                            },
                        },
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId2StGallenMajorityElectionInContestBund,
                            CandidateIds =
                            {
                                MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                            },
                        },
                        new MajorityElectionBallotGroupEntryCandidatesEventData
                        {
                            BallotGroupEntryId = MajorityElectionMockedData.BallotGroupEntryId3StGallenMajorityElectionInContestBund,
                        },
                    },
            },
        });

        var group = await RunOnDb(db => db.MajorityElectionBallotGroups
            .AsSplitQuery()
            .Include(x => x.Entries)
            .ThenInclude(x => x.Candidates)
            .FirstAsync(x =>
                x.Id == Guid.Parse(MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)));

        group.AllCandidateCountsOk.Should().BeFalse();
    }
}
