// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using eCH_0110_4_0;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Ech.Schemas;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0110ProportionalElectionTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0110ProportionalElectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantUzwil.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "eCH-0110_Kantonratswahl de.xml";

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);
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
                        Key = AusmittlungXmlProportionalElectionTemplates.Ech0110.Key,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(ProportionalElectionUnionEndResultMockedData.UzwilElectionId),
                        },
                    },
                },
        };
    }

    protected override XmlSchemaSet GetSchemaSet()
        => Ech0110SchemaLoader.LoadEch0110Schemas();

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
