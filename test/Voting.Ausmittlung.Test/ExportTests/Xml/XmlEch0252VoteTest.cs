// (c) Copyright 2024 by Abraxas Informatik AG
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

public class XmlEch0252VoteTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0252VoteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0252_votes_20290212.xml";

    [Fact]
    public async Task TestWithoutPublished()
    {
        await ModifyDbEntities<VoteResult>(x => x.Id == VoteEndResultMockedData.StGallenResultGuid, x => x.Published = false);
        await TestXmlWithSnapshot("WithoutPublished");
    }

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);
        await ModifyDbEntities<VoteResult>(_ => true, x => x.Published = true);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds =
            [
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlContestTemplates.VoteEch0252.Key,
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
