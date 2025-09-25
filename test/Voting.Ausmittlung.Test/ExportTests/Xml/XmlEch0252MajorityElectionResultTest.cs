// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0252_2_0;
using FluentAssertions;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Ech.Ech0252_2_0.Schemas;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0252MajorityElectionResultTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0252MajorityElectionResultTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0252_majority-election-result-delivery_20290212.xml";

    [Fact]
    public async Task TestWithoutPublished()
    {
        await ModifyDbEntities<MajorityElectionResult>(x => x.Id == MajorityElectionEndResultMockedData.StGallenResultGuid, x => x.Published = false);
        await TestXmlWithSnapshot("WithoutPublished");
    }

    [Fact]
    public async Task TestEVoting()
    {
        await ModifyDbEntities<Contest>(x => x.Id == ContestMockedData.GuidBundesurnengang, x => x.EVoting = true);
        await TestXmlWithSnapshot("EVoting");
    }

    [Fact]
    public async Task TestOwnedContestForeignMajorityElectionShouldBeIncluded()
    {
        // The contest belongs to the DoI/tenant "Bund", but the majority elections
        // belong to a different tenant. This should still work.
        var request = new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds =
            [
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlContestTemplates.MajorityElectionResultsEch0252.Key,
                    SecureConnectTestDefaults.MockedTenantBund.Id)

            ],
        };
        var httpClient = CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantBund.Id,
            roles: RolesMockedData.MonitoringElectionAdmin);

        var xml = await GetXml(httpClient, request);
        XmlUtil.ValidateSchema(xml, GetSchemaSet());
        MatchXmlSnapshot(xml, $"{GetType().Name}TestOwnedContestForeignMajorityElectionShouldBeIncluded");
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);
        await ModifyDbEntities<MajorityElectionResult>(_ => true, x => x.Published = true);
        await ModifyDbEntities<MajorityElectionCandidate>(
            x => x.Id == Guid.Parse(MajorityElectionEndResultMockedData.CandidateId1),
            x => x.CreatedDuringActiveContest = true);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds =
            [
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlContestTemplates.MajorityElectionResultsEch0252.Key,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id)

            ],
        };
    }

    protected override XmlSchemaSet GetSchemaSet()
        => Ech0252Schemas.LoadEch0252Schemas();

    protected override void AssertTestDeliveryFlag(Delivery delivery) => delivery.DeliveryHeader.TestDeliveryFlag.Should().BeFalse();

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
