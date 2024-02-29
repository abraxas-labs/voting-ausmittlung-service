// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
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
}
