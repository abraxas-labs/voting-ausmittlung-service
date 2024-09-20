// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ValidationTests;

public class ComparisonVoterParticipationValidationTest : BaseValidationTest<PoliticalBusinessNullableCountOfVotersValidator, PoliticalBusinessNullableCountOfVoters>
{
    public ComparisonVoterParticipationValidationTest()
        : base(SharedProto.Validation.ComparisonVoterParticipations)
    {
    }

    // Scenario from jira ticket 789
    private static List<VoteResult> MockVoteResults => new List<VoteResult>
    {
        BuildVoteResult(50, DomainOfInfluenceType.Ch),
        BuildVoteResult(60, DomainOfInfluenceType.Ch),
        BuildVoteResult(70, DomainOfInfluenceType.Ct),
        BuildVoteResult(60, DomainOfInfluenceType.Ct),
        BuildVoteResult(70, DomainOfInfluenceType.Mu),
    };

    [Fact]
    public void Test()
    {
        var voteResults = MockVoteResults;

        foreach (var ballotResult in voteResults.SelectMany(x => x.Results))
        {
            ballotResult.UpdateVoterParticipation(100);
        }

        var expectedCounts = new[] { 0, 1, 1, 2, 2 };

        var afterValidationResultStates = new[]
        {
            CountingCircleResultState.SubmissionDone,
            CountingCircleResultState.CorrectionDone,
            CountingCircleResultState.AuditedTentatively,
            CountingCircleResultState.Plausibilised,
            CountingCircleResultState.Plausibilised,
        };

        var countOfVotersList = voteResults.SelectMany(x => x.Results).Select(x => x.CountOfVoters).ToList();
        var context = BuildValidationContext(x => x.CurrentContestCountingCircleDetails.CountingCircle.VoteResults = voteResults);

        for (var i = 0; i < voteResults.Count; i++)
        {
            var voteResult = voteResults[i];
            context.PoliticalBusinessDomainOfInfluenceType = voteResult.Vote.DomainOfInfluence.Type;
            var validationResults = Validate(voteResult.Results.Single().CountOfVoters, context);

            EnsureHasCount(validationResults, expectedCounts[i]);

            validationResults.Where(x => x.Validation == SharedProto.Validation.ComparisonVoterParticipations)
                .ToList()
                .MatchSnapshot($"result-{i + 1}");
            voteResult.State = afterValidationResultStates[i];
        }
    }

    [Fact]
    public void ShouldReturnEmptyWhenThresholdIsNull()
    {
        var voteResults = MockVoteResults;
        var voteResult = voteResults[1];
        var context = BuildValidationContext(x =>
        {
            x.CurrentContestCountingCircleDetails.CountingCircle.VoteResults = voteResults;
            x.PoliticalBusinessDomainOfInfluenceType = voteResult.Vote.DomainOfInfluence.Type;
            x.PlausibilisationConfiguration!.ComparisonVoterParticipationConfigurations
                .Single(y => y.MainLevel == DomainOfInfluenceType.Ch && y.ComparisonLevel == DomainOfInfluenceType.Ch)
                .ThresholdPercent = null;
        });

        voteResults[0].State = CountingCircleResultState.SubmissionDone;
        var validationResults = Validate(voteResults[1].Results.Single().CountOfVoters, context);

        EnsureHasCount(validationResults, 0);
    }

    private static VoteResult BuildVoteResult(int conventionalReceivedBallots, DomainOfInfluenceType doiType = DomainOfInfluenceType.Ch)
    {
        return new VoteResult
        {
            Vote = new Vote
            {
                DomainOfInfluence = new DomainOfInfluence
                {
                    Type = doiType,
                },
            },
            Results = new List<BallotResult>
            {
                new() { CountOfVoters = new PoliticalBusinessNullableCountOfVoters { ConventionalReceivedBallots = conventionalReceivedBallots } },
            },
        };
    }

    private ValidationContext BuildValidationContext(Action<ValidationContext>? customizer = null)
    {
        return BuildValidationContext(customizer, null, PoliticalBusinessType.ProportionalElection);
    }
}
