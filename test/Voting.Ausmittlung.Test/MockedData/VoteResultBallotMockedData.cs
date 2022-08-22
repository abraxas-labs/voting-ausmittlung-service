// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.MockedData;

public static class VoteResultBallotMockedData
{
    public const string IdGossauBallot1 = "8afe2fa3-3edf-4149-bcba-a763077fa1bb";
    public const string IdGossauBallot2 = "57ea0846-c878-4bc3-9ec9-98e7dec1a10c";
    public const string IdGossauBallot10 = "b55ea5e7-9045-4ed4-986a-3b394eb3414b";

    public static VoteResultBallot GossauBallot1
        => new VoteResultBallot
        {
            Id = Guid.Parse(IdGossauBallot1),
            Number = 1,
            QuestionAnswers = new List<VoteResultBallotQuestionAnswer>
            {
                    new VoteResultBallotQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.BallotQuestion1IdGossauVoteInContestStGallen),
                        Answer = BallotQuestionAnswer.Yes,
                    },
                    new VoteResultBallotQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.BallotQuestion2IdGossauVoteInContestStGallen),
                        Answer = BallotQuestionAnswer.No,
                    },
            },
            TieBreakQuestionAnswers = new List<VoteResultBallotTieBreakQuestionAnswer>
            {
                    new VoteResultBallotTieBreakQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.TieBreakQuestionIdGossauVoteInContestStGallen),
                        Answer = TieBreakQuestionAnswer.Q1,
                    },
            },
            BundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
        };

    public static VoteResultBallot GossauBallot2
        => new VoteResultBallot
        {
            Id = Guid.Parse(IdGossauBallot2),
            Number = 2,
            QuestionAnswers = new List<VoteResultBallotQuestionAnswer>
            {
                    new VoteResultBallotQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.BallotQuestion1IdGossauVoteInContestStGallen),
                        Answer = BallotQuestionAnswer.Unspecified,
                    },
                    new VoteResultBallotQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.BallotQuestion2IdGossauVoteInContestStGallen),
                        Answer = BallotQuestionAnswer.Yes,
                    },
            },
            TieBreakQuestionAnswers = new List<VoteResultBallotTieBreakQuestionAnswer>
            {
                    new VoteResultBallotTieBreakQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.TieBreakQuestionIdGossauVoteInContestStGallen),
                        Answer = TieBreakQuestionAnswer.Q2,
                    },
            },
            BundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
        };

    public static VoteResultBallot GossauBallot10
        => new VoteResultBallot
        {
            Id = Guid.Parse(IdGossauBallot10),
            Number = 1,
            QuestionAnswers = new List<VoteResultBallotQuestionAnswer>
            {
                    new VoteResultBallotQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.BallotQuestion1IdGossauVoteInContestStGallen),
                        Answer = BallotQuestionAnswer.No,
                    },
                    new VoteResultBallotQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.BallotQuestion2IdGossauVoteInContestStGallen),
                        Answer = BallotQuestionAnswer.No,
                    },
            },
            TieBreakQuestionAnswers = new List<VoteResultBallotTieBreakQuestionAnswer>
            {
                    new VoteResultBallotTieBreakQuestionAnswer
                    {
                        QuestionId = Guid.Parse(VoteMockedData.TieBreakQuestionIdGossauVoteInContestStGallen),
                        Answer = TieBreakQuestionAnswer.Unspecified,
                    },
            },
            BundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle2),
        };

    public static IEnumerable<VoteResultBallot> All
    {
        get
        {
            yield return GossauBallot1;
            yield return GossauBallot2;
            yield return GossauBallot10;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.VoteResultBallots.AddRange(All);

            await db.SaveChangesAsync();
        });
    }
}
