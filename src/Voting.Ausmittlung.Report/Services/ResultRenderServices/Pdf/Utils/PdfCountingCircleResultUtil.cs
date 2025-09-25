// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfCountingCircleResultUtil
{
    public static void MapContestCountingCircleDetailsToResults(
        IEnumerable<PdfContestCountingCircleDetails> ccDetailsList,
        IEnumerable<PdfCountingCircleResult> results)
    {
        var ccDetailsById = ccDetailsList.ToDictionary(x => x.CountingCircleId);
        foreach (var result in results)
        {
            if (ccDetailsById.TryGetValue(result.CountingCircle!.Id, out var ccDetails))
            {
                result.CountingCircle!.ContestCountingCircleDetails = ccDetails;
            }
            else
            {
                result.CountingCircle!.ContestCountingCircleDetails = new PdfContestCountingCircleDetails();
            }
        }
    }

    public static void RemoveContactPersonDetails(IEnumerable<PdfCountingCircleResult> results)
    {
        foreach (var result in results)
        {
            result.CountingCircle!.ResponsibleAuthority = null;
            result.CountingCircle!.ContactPersonAfterEvent = null;
            result.CountingCircle!.ContactPersonDuringEvent = null;
            result.CountingCircle!.ContactPersonSameDuringEventAsAfter = null;
        }
    }

    internal static void ResetResultsIfNotDone<T>(List<T> results, List<ContestCountingCircleDetails> ccDetails)
        where T : CountingCircleResult
    {
        var doneCcIds = new HashSet<Guid>();
        var notDoneCcIds = new HashSet<Guid>();
        foreach (var result in results)
        {
            if (result.State.IsSubmissionDone())
            {
                doneCcIds.Add(result.CountingCircleId);
                continue;
            }

            result.ResetAllResults();
            notDoneCcIds.Add(result.CountingCircleId);
        }

        notDoneCcIds.ExceptWith(doneCcIds);
        var ccDetailByCcId = ccDetails.ToDictionary(x => x.CountingCircleId);
        foreach (var ccId in notDoneCcIds)
        {
            ccDetailByCcId.GetValueOrDefault(ccId)?.ResetVotingCardsAndSubTotals();
        }
    }
}
