// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ProportionalElectionResultBallotMockedData
{
    public const string IdUzwilBallot1 = "cd45df54-b093-492f-b3de-37e2c6f5c160";
    public const string IdUzwilBallot2 = "28fc0a51-eb49-4769-bad6-5aed6e6b2782";
    public const string IdUzwilBallot10 = "163fe899-0aea-4210-b7d4-d7248ed99252";

    public static ProportionalElectionResultBallot UzwilBallot1
        => new ProportionalElectionResultBallot
        {
            Number = 1,
            EmptyVoteCount = 2,
            BallotCandidates = new List<ProportionalElectionResultBallotCandidate>
            {
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil),
                        Position = 1,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId2UzwilProportionalElectionInContestUzwil),
                        Position = 2,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId11UzwilProportionalElectionInContestUzwil),
                        Position = 3,
                    },
            },
            BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle1),
        };

    public static ProportionalElectionResultBallot UzwilBallot2
        => new ProportionalElectionResultBallot
        {
            Number = 2,
            EmptyVoteCount = 4,
            BallotCandidates = new List<ProportionalElectionResultBallotCandidate>
            {
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil),
                        Position = 1,
                    },
            },
            BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle1),
        };

    public static ProportionalElectionResultBallot UzwilBallot11
        => new ProportionalElectionResultBallot
        {
            Number = 1,
            BallotCandidates = new List<ProportionalElectionResultBallotCandidate>
            {
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil),
                        OnList = true,
                        Position = 1,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil),
                        OnList = true,
                        RemovedFromList = true,
                        Position = 2,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId12UzwilProportionalElectionInContestUzwil),
                        Position = 2,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId2UzwilProportionalElectionInContestUzwil),
                        OnList = true,
                        RemovedFromList = true,
                        Position = 3,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId12UzwilProportionalElectionInContestUzwil),
                        Position = 3,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId3UzwilProportionalElectionInContestUzwil),
                        OnList = true,
                        Position = 4,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId11UzwilProportionalElectionInContestUzwil),
                        Position = 5,
                    },
            },
            BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle2),
        };

    public static IEnumerable<ProportionalElectionResultBallot> All
    {
        get
        {
            yield return UzwilBallot1;
            yield return UzwilBallot2;
            yield return UzwilBallot11;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.ProportionalElectionResultBallots.AddRange(All);

            await db.SaveChangesAsync();
        });
    }
}
