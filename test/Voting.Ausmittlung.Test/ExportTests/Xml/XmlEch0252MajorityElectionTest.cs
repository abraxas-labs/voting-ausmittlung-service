﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0110_4_0;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Ech.Ech0252_2_0.Schemas;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0252MajorityElectionTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0252MajorityElectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0252_majority-elections_20290212.xml";

    [Fact]
    public async Task TestWithoutPublished()
    {
        await ModifyDbEntities<MajorityElectionResult>(x => x.Id == MajorityElectionEndResultMockedData.StGallenResultGuid, x => x.Published = false);
        await TestXmlWithSnapshot("WithoutPublished");
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);
        await ModifyDbEntities<MajorityElectionResult>(_ => true, x => x.Published = true);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds =
            [
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlContestTemplates.MajorityElectionsEch0252.Key,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id)

            ],
        };
    }

    protected override XmlSchemaSet GetSchemaSet()
        => Ech0252Schemas.LoadEch0252Schemas();

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
