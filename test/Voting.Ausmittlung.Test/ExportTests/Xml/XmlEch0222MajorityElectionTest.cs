// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using eCH_0222_1_0.Standard;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0222MajorityElectionTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0222MajorityElectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0222_Mw SG de.xml";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SetBundlesReviewed();
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped, true);
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
        await MajorityElectionResultBallotMockedData.Seed(RunScoped);
        await MajorityElectionBallotGroupResultMockedData.Seed(RunScoped);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ResultExportRequests =
                {
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungXmlMajorityElectionTemplates.Ech0222.Key,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
                        },
                    },
                },
        };
    }

    protected override void CleanDataForSnapshot(Delivery data)
    {
        // the eai ech lib uses yyyy-MM-ddTHH:mm:ss.fff internally
        // with our default mocked timestamp of 2020-01-10T13:12:10.200
        // this sometimes leads to 2020-01-10T13:12:10.2 and sometimes to 2020-01-10T13:12:10.200
        // and results in flaky tests...
        // no idea why the format is different (even on the same machine random for each call)
        // with a fixed 3 digits ms part it should be resolved.
        data.DeliveryHeader.MessageDate = "2020-01-10T13:12:10.123";
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private async Task SetBundlesReviewed()
    {
        await RunOnDb(async db =>
        {
            var bundles = await db.MajorityElectionResultBundles
                .AsTracking()
                .Where(x => x.ElectionResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))
                .ToListAsync();

            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            await db.SaveChangesAsync();
        });
    }
}
