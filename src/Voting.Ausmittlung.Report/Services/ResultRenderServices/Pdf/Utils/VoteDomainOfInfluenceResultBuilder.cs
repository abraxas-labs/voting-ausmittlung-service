// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public class VoteDomainOfInfluenceResultBuilder
    : DomainOfInfluenceResultBuilder<Vote, VoteDomainOfInfluenceResult, VoteResult>
{
    internal static readonly PropertyEqualityComparer<Ballot, Guid> BallotComparer = new(b => b.Id);

    public VoteDomainOfInfluenceResultBuilder(DomainOfInfluenceRepo doiRepo)
        : base(doiRepo)
    {
    }

    public async Task<(IEnumerable<IGrouping<Ballot, VoteDomainOfInfluenceBallotResult>> Results, VoteDomainOfInfluenceResult NotAssignableResult, VoteDomainOfInfluenceResult AggregatedResult)> BuildResultsGroupedByBallot(
        Vote vote,
        List<ContestCountingCircleDetails> ccDetails,
        string tenantId,
        HashSet<Guid>? viewablePartialResultsCountingCircleIds)
    {
        var (results, notAssignableResult, aggregatedResult) = await BuildResults(vote, ccDetails, tenantId, viewablePartialResultsCountingCircleIds);
        MapContestDetails(notAssignableResult);
        MapContestDetails(aggregatedResult);
        foreach (var result in results)
        {
            MapContestDetails(result);
        }

        var groupedResults = results
            .SelectMany(x => x.BallotResults)
            .GroupBy(x => x.Ballot, x => x, BallotComparer)
            .OrderBy(x => x.Key.Position);
        return (groupedResults, notAssignableResult, aggregatedResult);
    }

    internal void ApplyVoteCountingCircleResult(VoteDomainOfInfluenceResult doiResult, VoteResult ccResult)
        => ApplyCountingCircleResult(doiResult, ccResult);

    internal void MapContestDetails(VoteDomainOfInfluenceResult result)
    {
        foreach (var ballotResult in result.BallotResults)
        {
            ballotResult.ContestDomainOfInfluenceDetails.TotalCountOfVoters =
                result.ContestDomainOfInfluenceDetails.TotalCountOfVoters;
            ballotResult.ContestDomainOfInfluenceDetails.TotalCountOfValidVotingCards =
                result.ContestDomainOfInfluenceDetails.TotalCountOfValidVotingCards;
            ballotResult.ContestDomainOfInfluenceDetails.TotalCountOfInvalidVotingCards =
                result.ContestDomainOfInfluenceDetails.TotalCountOfInvalidVotingCards;
        }
    }

    protected override IEnumerable<VoteResult> GetResults(Vote politicalBusiness) => politicalBusiness.Results;

    protected override async Task<int> GetReportLevel(Vote politicalBusiness, HashSet<Guid>? viewablePartialResultsCountingCircleIds)
    {
        if (viewablePartialResultsCountingCircleIds == null)
        {
            return politicalBusiness.ReportDomainOfInfluenceLevel;
        }

        return await GetPartialResultReportLevel(
            politicalBusiness.ReportDomainOfInfluenceLevel,
            politicalBusiness.DomainOfInfluenceId,
            viewablePartialResultsCountingCircleIds);
    }

    protected override void ApplyCountingCircleResult(
        VoteDomainOfInfluenceResult doiResult,
        VoteResult ccResult)
    {
        foreach (var result in ccResult.Results)
        {
            ApplyCountingCircleResult(doiResult, result, 1);
            var doiBallotResult = doiResult.ResultsByBallotId[result.BallotId];
            doiBallotResult.AddResult(result);
        }

        doiResult.VoteResults.Add(ccResult);
    }

    protected override void ApplyCountingCircleResultToVirtualResult(VoteResult target, VoteResult ccResult)
    {
        if (ccResult.State < target.State)
        {
            target.State = ccResult.State;
        }

        target.Published &= ccResult.Published;
        target.TotalCountOfVoters += ccResult.TotalCountOfVoters;

        if (target.TotalSentEVotingVotingCards == null && ccResult.TotalSentEVotingVotingCards != null)
        {
            target.TotalSentEVotingVotingCards = ccResult.TotalSentEVotingVotingCards;
        }
        else if (target.TotalSentEVotingVotingCards != null)
        {
            target.TotalSentEVotingVotingCards += ccResult.TotalSentEVotingVotingCards ?? 0;
        }

        var targetResultsByBallotId = target.Results.ToDictionary(x => x.BallotId);
        foreach (var ccBallotResult in ccResult.Results)
        {
            if (!targetResultsByBallotId.TryGetValue(ccBallotResult.BallotId, out var targetBallotResult))
            {
                targetBallotResult = targetResultsByBallotId[ccBallotResult.BallotId] = new BallotResult
                {
                    BallotId = ccBallotResult.BallotId,
                    Ballot = ccBallotResult.Ballot,
                    QuestionResults = ccBallotResult.QuestionResults
                        .Select(x => new BallotQuestionResult
                        {
                            Question = x.Question,
                            QuestionId = x.QuestionId,
                        })
                        .ToList(),
                    TieBreakQuestionResults = ccBallotResult.TieBreakQuestionResults
                        .Select(x => new TieBreakQuestionResult
                        {
                            Question = x.Question,
                            QuestionId = x.QuestionId,
                        })
                        .ToList(),
                };

                targetBallotResult.VoteResult = target;
                target.Results.Add(targetBallotResult);
            }

            targetBallotResult.CountOfVoters.AddForAllSubTotals(ccBallotResult.CountOfVoters);

            var targetQuestionResultById = targetBallotResult.QuestionResults.ToDictionary(x => x.QuestionId);
            var targetTieBreakResultById = targetBallotResult.TieBreakQuestionResults.ToDictionary(x => x.QuestionId);

            foreach (var ccQuestionResult in ccBallotResult.QuestionResults)
            {
                var targetQuestionResult = targetQuestionResultById[ccQuestionResult.QuestionId];
                targetQuestionResult.AddForAllSubTotals(ccQuestionResult);
            }

            foreach (var ccTieBreakResult in ccBallotResult.TieBreakQuestionResults)
            {
                var targetTieBreakResult = targetTieBreakResultById[ccTieBreakResult.QuestionId];
                targetTieBreakResult.AddForAllSubTotals(ccTieBreakResult);
            }

            targetBallotResult.CountOfBundlesNotReviewedOrDeleted += ccBallotResult.CountOfBundlesNotReviewedOrDeleted;
            targetBallotResult.ConventionalCountOfDetailedEnteredBallots += ccBallotResult.ConventionalCountOfDetailedEnteredBallots;
        }

        target.UpdateVoterParticipation();
    }

    protected override IEnumerable<VoteResult> GetCountingCircleResults(VoteDomainOfInfluenceResult doiResult)
        => doiResult.VoteResults;

    protected override void RemoveCountingCircleResult(VoteDomainOfInfluenceResult doiResult, VoteResult result)
    {
        foreach (var ballotResult in result.Results)
        {
            ApplyCountingCircleResult(doiResult, ballotResult, -1);
            var doiBallotResult = doiResult.ResultsByBallotId[ballotResult.BallotId];
            doiBallotResult.RemoveResult(ballotResult);
        }

        doiResult.VoteResults.Remove(result);
    }

    protected override void ResetCountingCircleResult(VoteResult ccResult)
    {
        ccResult.ResetAllResults();
    }

    private void ApplyCountingCircleResult(
        VoteDomainOfInfluenceResult doiResult,
        BallotResult result,
        int deltaFactor)
    {
        if (!doiResult.ResultsByBallotId.TryGetValue(result.BallotId, out var doiBallotResult))
        {
            doiBallotResult = doiResult.ResultsByBallotId[result.BallotId] =
                new VoteDomainOfInfluenceBallotResult(result.Ballot, doiResult.DomainOfInfluence);
        }

        PoliticalBusinessCountOfVotersUtils.AdjustCountOfVoters(
            doiBallotResult.CountOfVoters,
            result.CountOfVoters,
            doiResult.ContestDomainOfInfluenceDetails.TotalCountOfVoters,
            deltaFactor);

        foreach (var questionResult in result.QuestionResults)
        {
            ApplyQuestionResult(
                doiBallotResult.QuestionResultsByQuestionId[questionResult.QuestionId],
                questionResult,
                deltaFactor);
        }

        foreach (var tieBreakQuestionResult in result.TieBreakQuestionResults)
        {
            doiBallotResult.TieBreakQuestionResultsByQuestionId[tieBreakQuestionResult.QuestionId].AddForAllSubTotals(tieBreakQuestionResult, deltaFactor);
        }
    }

    private void ApplyQuestionResult(
        BallotQuestionDomainOfInfluenceResult doiResult,
        BallotQuestionResult ccResult,
        int deltaFactor)
    {
        doiResult.AddForAllSubTotals(ccResult, deltaFactor);
        if (ccResult.TotalCountOfAnswerYes != 0 || ccResult.TotalCountOfAnswerYes != 0)
        {
            if (ccResult.HasMajority)
            {
                doiResult.CountOfCountingCircleYes += deltaFactor;
            }
            else
            {
                doiResult.CountOfCountingCircleNo += deltaFactor;
            }
        }
    }
}
