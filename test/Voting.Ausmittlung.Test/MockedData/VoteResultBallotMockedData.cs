// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.MockedData;

public static class VoteResultBallotMockedData
{
    public const string IdGossauBallot1 = "8afe2fa3-3edf-4149-bcba-a763077fa1bb";
    public const string IdGossauBallot2 = "57ea0846-c878-4bc3-9ec9-98e7dec1a10c";
    public const string IdGossauBallot10 = "b55ea5e7-9045-4ed4-986a-3b394eb3414b";
    public const string IdGossauBallot30 = "adf5508c-c37f-4282-8d34-bf5650578acb";

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

    public static VoteResultBallot GossauBallot30
        => new VoteResultBallot
        {
            Id = Guid.Parse(IdGossauBallot30),
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
            BundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle3),
        };

    public static IEnumerable<VoteResultBallot> All
    {
        get
        {
            yield return GossauBallot1;
            yield return GossauBallot2;
            yield return GossauBallot10;
            yield return GossauBallot30;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.VoteResultBallots.AddRange(All);

            await db.SaveChangesAsync();

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", Enumerable.Empty<string>());

            var ballots = await db.VoteResultBallots
                .AsSplitQuery()
                .Include(x => x.Bundle.BallotResult.VoteResult.Vote)
                .Include(x => x.QuestionAnswers)
                .ThenInclude(x => x.Question)
                .Include(x => x.TieBreakQuestionAnswers)
                .ThenInclude(x => x.Question)
                .ToListAsync();

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            foreach (var ballot in ballots)
            {
                var bundleAggregate = await aggregateRepository.GetById<VoteResultBundleAggregate>(ballot.BundleId);
                var questionAnswers = ballot.QuestionAnswers
                    .Select(x => new Core.Domain.VoteResultBallotQuestionAnswer
                    {
                        QuestionNumber = x.Question.Number,
                        Answer = x.Answer,
                    })
                    .ToList();
                var tieBreakQuestionAnswers = ballot.TieBreakQuestionAnswers
                    .Select(x => new Core.Domain.VoteResultBallotTieBreakQuestionAnswer
                    {
                        QuestionNumber = x.Question.Number,
                        Answer = x.Answer,
                    })
                    .ToList();
                bundleAggregate.CreateBallot(questionAnswers, tieBreakQuestionAnswers, ballot.Bundle.BallotResult.VoteResult.Vote.ContestId);
                await aggregateRepository.Save(bundleAggregate);
            }
        });
    }
}
