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
        List<ContestCountingCircleDetails> ccDetails)
    {
        var (results, notAssignableResult, aggregatedResult) = await BuildResults(vote, ccDetails);
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

    protected override int GetReportLevel(Vote politicalBusiness) => politicalBusiness.ReportDomainOfInfluenceLevel;

    protected override void ApplyCountingCircleResult(
        VoteDomainOfInfluenceResult doiResult,
        VoteResult ccResult)
    {
        foreach (var result in ccResult.Results)
        {
            if (!doiResult.ResultsByBallotId.TryGetValue(result.BallotId, out var doiBallotResult))
            {
                doiBallotResult = doiResult.ResultsByBallotId[result.BallotId] =
                    new VoteDomainOfInfluenceBallotResult(result.Ballot, doiResult.DomainOfInfluence);
            }

            PoliticalBusinessCountOfVotersUtils.AdjustCountOfVoters(
                doiBallotResult.CountOfVoters,
                result.CountOfVoters,
                doiResult.ContestDomainOfInfluenceDetails.TotalCountOfVoters);

            foreach (var questionResult in result.QuestionResults)
            {
                ApplyQuestionResult(
                    doiBallotResult.QuestionResultsByQuestionId[questionResult.QuestionId],
                    questionResult);
            }

            foreach (var tieBreakQuestionResult in result.TieBreakQuestionResults)
            {
                ApplyTieBreakQuestionResult(
                    doiBallotResult.TieBreakQuestionResultsByQuestionId[tieBreakQuestionResult.QuestionId],
                    tieBreakQuestionResult);
            }

            doiBallotResult.AddResult(result);
        }
    }

    protected override void ResetCountingCircleResult(VoteResult ccResult)
    {
        ccResult.TotalCountOfVoters = 0;

        ccResult.ResetAllSubTotals(VotingDataSource.Conventional, true);
        ccResult.ResetAllSubTotals(VotingDataSource.EVoting, true);
    }

    private void ApplyQuestionResult(BallotQuestionDomainOfInfluenceResult doiResult, BallotQuestionResult ccResult)
    {
        doiResult.ForEachSubTotal(ccResult, (doiSubTotal, ccSubTotal) =>
        {
            doiSubTotal.TotalCountOfAnswerYes += ccSubTotal.TotalCountOfAnswerYes;
            doiSubTotal.TotalCountOfAnswerNo += ccSubTotal.TotalCountOfAnswerNo;
            doiSubTotal.TotalCountOfAnswerUnspecified += ccSubTotal.TotalCountOfAnswerUnspecified;
        });
    }

    private void ApplyTieBreakQuestionResult(TieBreakQuestionDomainOfInfluenceResult doiResult, TieBreakQuestionResult ccResult)
    {
        doiResult.ForEachSubTotal(ccResult, (doiSubTotal, ccSubTotal) =>
        {
            doiSubTotal.TotalCountOfAnswerQ1 += ccSubTotal.TotalCountOfAnswerQ1;
            doiSubTotal.TotalCountOfAnswerQ2 += ccSubTotal.TotalCountOfAnswerQ2;
            doiSubTotal.TotalCountOfAnswerUnspecified += ccSubTotal.TotalCountOfAnswerUnspecified;
        });
    }
}
