// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfMajorityElectionEndResultExportBaseTest : PdfExportBaseTest
{
    protected PdfMajorityElectionEndResultExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);
    }

    protected override async Task<bool> SetToSubmissionOngoing()
    {
        var majorityElectionId = Guid.Parse(MajorityElectionEndResultMockedData.ElectionId);
        var results = await RunOnDb<List<MajorityElectionResult>>(db => db.MajorityElectionResults
            .Include(x => x.MajorityElection)
            .Include(x => x.CountingCircle)
            .Where(x => x.MajorityElectionId == majorityElectionId)
            .ToListAsync());

        var ccDetailsEvents = results.Select(x => new ContestCountingCircleDetailsResetted
        {
            Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(x.MajorityElection.ContestId, x.CountingCircle.BasisCountingCircleId, false).ToString(),
            ContestId = x.MajorityElection.ContestId.ToString(),
            CountingCircleId = x.CountingCircle.BasisCountingCircleId.ToString(),
            EventInfo = GetMockedEventInfo(),
        }).ToArray();
        await TestEventPublisher.Publish(ccDetailsEvents);

        var meEvents = results.Select(x => new MajorityElectionResultResetted
        {
            ElectionResultId = x.Id.ToString(),
            EventInfo = GetMockedEventInfo(),
        }).ToArray();
        await TestEventPublisher.Publish(ccDetailsEvents.Length, meEvents);
        return true;
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
