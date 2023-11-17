// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfProportionalElectionResultUtil
{
    public static void SetTotalListResults(PdfProportionalElectionResult result)
    {
        result.TotalListResult = new PdfProportionalElectionListResult
        {
            TotalVoteCount = result.ListResults.Sum(x => x.TotalVoteCount),
            BlankRowsCount = result.ListResults.Sum(x => x.BlankRowsCount),
            ListCount = result.ListResults.Sum(x => x.ListCount),
            ListVotesCount = result.ListResults.Sum(x => x.ListVotesCount),
            ModifiedListsCount = result.ListResults.Sum(x => x.ModifiedListsCount),
            UnmodifiedListsCount = result.ListResults.Sum(x => x.UnmodifiedListsCount),
            ModifiedListVotesCount = result.ListResults.Sum(x => x.ModifiedListVotesCount),
            UnmodifiedListVotesCount = result.ListResults.Sum(x => x.UnmodifiedListVotesCount),
            ModifiedListBlankRowsCount = result.ListResults.Sum(x => x.ModifiedListBlankRowsCount),
            UnmodifiedListBlankRowsCount = result.ListResults.Sum(x => x.UnmodifiedListBlankRowsCount),
            ModifiedListVotesCountInclBlankRows = result.ListResults.Sum(x => x.ModifiedListVotesCountInclBlankRows),
            UnmodifiedListVotesCountInclBlankRows = result.ListResults.Sum(x => x.UnmodifiedListVotesCountInclBlankRows),
            ConventionalSubTotal = SetTotalListResultSubTotal(result.ListResults.ConvertAll(x => x.ConventionalSubTotal)!),
            EVotingSubTotal = SetTotalListResultSubTotal(result.ListResults.ConvertAll(x => x.EVotingSubTotal)!),
        };
        result.TotalListResultInclWithoutParty = new PdfProportionalElectionListResult
        {
            TotalVoteCount = result.TotalListResult.TotalVoteCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            BlankRowsCount = result.TotalListResult.BlankRowsCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            ListCount = result.TotalListResult.ListCount + result.TotalCountOfListsWithoutParty,
            ListVotesCount = result.TotalListResult.ListVotesCount,
            ModifiedListsCount = result.TotalListResult.ModifiedListsCount + result.TotalCountOfListsWithoutParty,
            UnmodifiedListsCount = result.TotalListResult.UnmodifiedListsCount,
            ModifiedListVotesCount = result.TotalListResult.ModifiedListVotesCount,
            UnmodifiedListVotesCount = result.TotalListResult.UnmodifiedListVotesCount,
            ModifiedListBlankRowsCount = result.TotalListResult.ModifiedListBlankRowsCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            UnmodifiedListBlankRowsCount = result.TotalListResult.UnmodifiedListBlankRowsCount,
            ModifiedListVotesCountInclBlankRows = result.TotalListResult.ModifiedListVotesCountInclBlankRows,
            UnmodifiedListVotesCountInclBlankRows = result.TotalListResult.UnmodifiedListVotesCountInclBlankRows,
            ConventionalSubTotal = SetTotalListResultSubTotalInclWithoutParty(result.TotalListResult.ConventionalSubTotal, result.ConventionalSubTotal!),
            EVotingSubTotal = SetTotalListResultSubTotalInclWithoutParty(result.TotalListResult.EVotingSubTotal, result.EVotingSubTotal!),
        };
    }

    public static PdfProportionalElectionListResultSubTotal SetTotalListResultSubTotal(
        IReadOnlyCollection<PdfProportionalElectionListResultSubTotal> subTotals)
    {
        return new PdfProportionalElectionListResultSubTotal
        {
            TotalVoteCount = subTotals.Sum(x => x.TotalVoteCount),
            BlankRowsCount = subTotals.Sum(x => x.BlankRowsCount),
            ListCount = subTotals.Sum(x => x.ListCount),
            ListVotesCount = subTotals.Sum(x => x.ListVotesCount),
            ModifiedListsCount = subTotals.Sum(x => x.ModifiedListsCount),
            UnmodifiedListsCount = subTotals.Sum(x => x.UnmodifiedListsCount),
            ModifiedListVotesCount = subTotals.Sum(x => x.ModifiedListVotesCount),
            UnmodifiedListVotesCount = subTotals.Sum(x => x.UnmodifiedListVotesCount),
            ModifiedListBlankRowsCount = subTotals.Sum(x => x.ModifiedListBlankRowsCount),
            UnmodifiedListBlankRowsCount = subTotals.Sum(x => x.UnmodifiedListBlankRowsCount),
            ModifiedListVotesCountInclBlankRows = subTotals.Sum(x => x.ModifiedListVotesCountInclBlankRows),
            UnmodifiedListVotesCountInclBlankRows = subTotals.Sum(x => x.UnmodifiedListVotesCountInclBlankRows),
        };
    }

    public static PdfProportionalElectionListResultSubTotal SetTotalListResultSubTotalInclWithoutParty(
        PdfProportionalElectionListResultSubTotal totalResultSubTotal,
        PdfProportionalElectionResultSubTotal resultSubTotal)
    {
        return new PdfProportionalElectionListResultSubTotal
        {
            TotalVoteCount = totalResultSubTotal.TotalVoteCount + resultSubTotal.TotalCountOfBlankRowsOnListsWithoutParty,
            BlankRowsCount = totalResultSubTotal.BlankRowsCount + resultSubTotal.TotalCountOfBlankRowsOnListsWithoutParty,
            ListCount = totalResultSubTotal.ListCount + resultSubTotal.TotalCountOfListsWithoutParty,
            ListVotesCount = totalResultSubTotal.ListVotesCount,
            ModifiedListsCount = totalResultSubTotal.ModifiedListsCount + resultSubTotal.TotalCountOfListsWithoutParty,
            UnmodifiedListsCount = totalResultSubTotal.UnmodifiedListsCount,
            ModifiedListVotesCount = totalResultSubTotal.ModifiedListVotesCount,
            UnmodifiedListVotesCount = totalResultSubTotal.UnmodifiedListVotesCount,
            ModifiedListBlankRowsCount = totalResultSubTotal.ModifiedListBlankRowsCount + resultSubTotal.TotalCountOfBlankRowsOnListsWithoutParty,
            UnmodifiedListBlankRowsCount = totalResultSubTotal.UnmodifiedListBlankRowsCount,
            ModifiedListVotesCountInclBlankRows = totalResultSubTotal.ModifiedListVotesCountInclBlankRows,
            UnmodifiedListVotesCountInclBlankRows = totalResultSubTotal.UnmodifiedListVotesCountInclBlankRows,
        };
    }
}
