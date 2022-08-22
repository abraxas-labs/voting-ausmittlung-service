// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using eCH_0110_4_0;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0110VoteTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0110VoteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0110_Abst SG de.xml";

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);
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
                        Key = AusmittlungXmlVoteTemplates.Ech0110.Key,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(VoteEndResultMockedData.VoteId),
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
}
