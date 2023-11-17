// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

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
            ModifiedListVotesCountInclBlankRows = result.ListEndResults.Sum(x => x.ModifiedListVotesCountInclBlankRows),
            UnmodifiedListVotesCountInclBlankRows = result.ListEndResults.Sum(x => x.UnmodifiedListVotesCountInclBlankRows),
            ConventionalSubTotal = PdfProportionalElectionResultUtil.SetTotalListResultSubTotal(result.ListEndResults.ConvertAll(x => x.ConventionalSubTotal)!),
            EVotingSubTotal = PdfProportionalElectionResultUtil.SetTotalListResultSubTotal(result.ListEndResults.ConvertAll(x => x.EVotingSubTotal)!),
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
            ModifiedListVotesCountInclBlankRows = result.TotalListEndResult.ModifiedListVotesCountInclBlankRows,
            UnmodifiedListVotesCountInclBlankRows = result.TotalListEndResult.UnmodifiedListVotesCountInclBlankRows,
            ConventionalSubTotal = PdfProportionalElectionResultUtil.SetTotalListResultSubTotalInclWithoutParty(result.TotalListEndResult.ConventionalSubTotal, result.ConventionalSubTotal!),
            EVotingSubTotal = PdfProportionalElectionResultUtil.SetTotalListResultSubTotalInclWithoutParty(result.TotalListEndResult.EVotingSubTotal, result.EVotingSubTotal!),
        };
    }
}
