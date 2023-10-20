// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfProportionalElectionEndResultUtil
{
    public static void SetTotalListResults(PdfProportionalElectionEndResult result)
    {
        result.TotalListEndResult = new PdfProportionalElectionListEndResult
        {
            NumberOfMandates = result.ListEndResults.Sum(x => x.NumberOfMandates),
            TotalVoteCount = result.ListEndResults.Sum(x => x.TotalVoteCount),
            BlankRowsCount = result.ListEndResults.Sum(x => x.BlankRowsCount),
            ListCount = result.ListEndResults.Sum(x => x.ListCount),
            ListVotesCount = result.ListEndResults.Sum(x => x.ListVotesCount),
            ModifiedListsCount = result.ListEndResults.Sum(x => x.ModifiedListsCount),
            UnmodifiedListsCount = result.ListEndResults.Sum(x => x.UnmodifiedListsCount),
            ModifiedListVotesCount = result.ListEndResults.Sum(x => x.ModifiedListVotesCount),
            UnmodifiedListVotesCount = result.ListEndResults.Sum(x => x.UnmodifiedListVotesCount),
            ModifiedListBlankRowsCount = result.ListEndResults.Sum(x => x.ModifiedListBlankRowsCount),
            UnmodifiedListBlankRowsCount = result.ListEndResults.Sum(x => x.UnmodifiedListBlankRowsCount),
            ConventionalSubTotal = SetTotalListResultSubTotal(result.ListEndResults.ConvertAll(x => x.ConventionalSubTotal)!),
            EVotingSubTotal = SetTotalListResultSubTotal(result.ListEndResults.ConvertAll(x => x.EVotingSubTotal)!),
        };
        result.TotalListResultInclWithoutParty = new PdfProportionalElectionListEndResult
        {
            TotalVoteCount = result.TotalListEndResult.TotalVoteCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            BlankRowsCount = result.TotalListEndResult.BlankRowsCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            ListCount = result.TotalListEndResult.ListCount + result.TotalCountOfListsWithoutParty,
            ListVotesCount = result.TotalListEndResult.ListVotesCount,
            ModifiedListsCount = result.TotalListEndResult.ModifiedListsCount + result.TotalCountOfListsWithoutParty,
            UnmodifiedListsCount = result.TotalListEndResult.UnmodifiedListsCount,
            ModifiedListVotesCount = result.TotalListEndResult.ModifiedListVotesCount,
            UnmodifiedListVotesCount = result.TotalListEndResult.UnmodifiedListVotesCount,
            ModifiedListBlankRowsCount = result.TotalListEndResult.ModifiedListBlankRowsCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            UnmodifiedListBlankRowsCount = result.TotalListEndResult.UnmodifiedListBlankRowsCount,
            ConventionalSubTotal = SetTotalListResultSubTotalInclWithoutParty(result.TotalListEndResult.ConventionalSubTotal, result.ConventionalSubTotal!),
            EVotingSubTotal = SetTotalListResultSubTotalInclWithoutParty(result.TotalListEndResult.EVotingSubTotal, result.EVotingSubTotal!),
        };
    }

    private static PdfProportionalElectionListResultSubTotal SetTotalListResultSubTotal(
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
        };
    }

    private static PdfProportionalElectionListResultSubTotal SetTotalListResultSubTotalInclWithoutParty(
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
        };
    }
}
