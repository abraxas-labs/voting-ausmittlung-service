// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public abstract class VoteProcessorBaseTest : BaseDataProcessorTest
{
    protected VoteProcessorBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
    }

    protected Task<List<Vote>> GetData()
        => GetData(_ => true);

    protected async Task<List<Vote>> GetData(Expression<Func<Vote, bool>> predicate, bool resetQuestionIds = false)
    {
        var data = await RunOnDb(
            db => db.Votes
                .AsSplitQuery()
                .Where(predicate)
                .Include(x => x.Translations)
                .Include(x => x.Ballots).ThenInclude(x => x.Translations)
                .Include(x => x.Ballots).ThenInclude(x => x.BallotQuestions).ThenInclude(x => x.Translations)
                .Include(x => x.Ballots).ThenInclude(x => x.TieBreakQuestions).ThenInclude(x => x.Translations)
                .OrderBy(x => x.PoliticalBusinessNumber)
                .ToListAsync(),
            Languages.German);

        foreach (var vote in data)
        {
            RemoveDynamicData(vote);
            vote.Ballots = vote.Ballots
                .OrderBy(b => b.Position)
                .ToList();

            foreach (var voteBallot in vote.Ballots)
            {
                SetDynamicIdToDefaultValue(voteBallot.Translations);

                voteBallot.BallotQuestions = voteBallot.BallotQuestions
                    .OrderBy(x => x.Number)
                    .ToList();
                voteBallot.TieBreakQuestions = voteBallot.TieBreakQuestions
                    .OrderBy(x => x.Number)
                    .ToList();

                SetDynamicIdToDefaultValue(voteBallot.BallotQuestions.SelectMany(x => x.Translations));
                SetDynamicIdToDefaultValue(voteBallot.TieBreakQuestions.SelectMany(x => x.Translations));
            }
        }

        if (resetQuestionIds)
        {
            ResetQuestionIds(data);
        }

        return data;
    }

    private void ResetQuestionIds(IEnumerable<Vote> votes)
    {
        foreach (var ballot in votes.SelectMany(vote => vote.Ballots))
        {
            foreach (var question in ballot.BallotQuestions)
            {
                question.Id = Guid.Empty;
                foreach (var translation in question.Translations)
                {
                    translation.BallotQuestionId = Guid.Empty;
                }
            }

            foreach (var question in ballot.TieBreakQuestions)
            {
                question.Id = Guid.Empty;
                foreach (var translation in question.Translations)
                {
                    translation.TieBreakQuestionId = Guid.Empty;
                }
            }
        }
    }
}
