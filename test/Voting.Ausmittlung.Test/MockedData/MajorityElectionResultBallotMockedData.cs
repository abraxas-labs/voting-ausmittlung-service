// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.MockedData;

public static class MajorityElectionResultBallotMockedData
{
    public const string IdStGallenBallot1 = "54a096d4-3420-4de2-88a9-2964afaf8017";
    public const string IdStGallenBallot2 = "5f6c326f-ce27-4ab8-989b-b9a43d55ac2b";
    public const string IdStGallenBallot3 = "17e2ecaa-2ac7-466d-97ca-ed6aa5b1d913";

    public const string IdStGallenBallotCandidate1 = "3001bec0-e077-470b-b250-312f31802658";

    public static MajorityElectionResultBallot StGallenBallot1
        => new MajorityElectionResultBallot
        {
            Id = Guid.Parse(IdStGallenBallot1),
            Number = 1,
            BallotCandidates = new List<MajorityElectionResultBallotCandidate>
            {
                    new MajorityElectionResultBallotCandidate
                    {
                        Id = Guid.Parse(IdStGallenBallotCandidate1),
                        CandidateId = Guid.Parse(MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund),
                        BallotId = Guid.Parse(IdStGallenBallot1),
                        Selected = true,
                    },
            },
            BundleId = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1),
        };

    public static MajorityElectionResultBallot StGallenBallot2
        => new MajorityElectionResultBallot
        {
            Id = Guid.Parse(IdStGallenBallot2),
            Number = 2,
            IndividualVoteCount = 1,
            BundleId = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1),
        };

    public static MajorityElectionResultBallot StGallenBallot3
        => new MajorityElectionResultBallot
        {
            Id = Guid.Parse(IdStGallenBallot3),
            Number = 3,
            EmptyVoteCount = 1,
            BundleId = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1),
        };

    public static IEnumerable<MajorityElectionResultBallot> All
    {
        get
        {
            yield return StGallenBallot1;
            yield return StGallenBallot2;
            yield return StGallenBallot3;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.MajorityElectionResultBallots.AddRange(All);

            await db.SaveChangesAsync();
        });
    }
}
