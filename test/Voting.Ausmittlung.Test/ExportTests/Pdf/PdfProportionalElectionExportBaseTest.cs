// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfProportionalElectionExportBaseTest : PdfExportBaseTest
{
    protected PdfProportionalElectionExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected async Task<bool> SetToSubmissionOngoing(params Guid[] electionIds)
    {
        var results = await RunOnDb(db => db.ProportionalElectionResults
            .Include(x => x.ProportionalElection)
            .Include(x => x.CountingCircle)
            .Where(x => electionIds.Contains(x.ProportionalElectionId) && x.State >= CountingCircleResultState.SubmissionDone)
            .ToListAsync());

        var ccDetailsEvents = results.Select(x => new ContestCountingCircleDetailsResetted
        {
            Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(x.ProportionalElection.ContestId, x.CountingCircle.BasisCountingCircleId, false).ToString(),
            ContestId = x.ProportionalElection.ContestId.ToString(),
            CountingCircleId = x.CountingCircle.BasisCountingCircleId.ToString(),
            EventInfo = GetMockedEventInfo(),
        }).ToArray();
        await TestEventPublisher.Publish(ccDetailsEvents);

        var peEvents = results.Select(x => new ProportionalElectionResultResetted
        {
            ElectionResultId = x.Id.ToString(),
            EventInfo = GetMockedEventInfo(),
        }).ToArray();
        await TestEventPublisher.Publish(ccDetailsEvents.Length, peEvents);
        return true;
    }
}
