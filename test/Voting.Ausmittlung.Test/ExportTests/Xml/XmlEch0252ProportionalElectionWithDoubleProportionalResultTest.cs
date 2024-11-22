// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0252_2_0;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Ech.Ech0252_2_0.Schemas;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0252ProportionalElectionWithDoubleProportionalResultTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0252ProportionalElectionWithDoubleProportionalResultTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => BundMonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0252_proportional-election-result-delivery_20290212.xml";

    protected override async Task SeedData()
    {
        await ZhMockedData.Seed(RunScoped, true);
        await RunScoped<IServiceProvider>(async sp =>
        {
            var dpResultBuilder = sp.GetRequiredService<DoubleProportionalResultBuilder>();
            await dpResultBuilder.BuildForElection(ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot);
            await dpResultBuilder.BuildForUnion(ZhMockedData.ProportionalElectionUnionGuidSuperLot);
            await dpResultBuilder.BuildForUnion(ZhMockedData.ProportionalElectionUnionGuidSubLot);
        });
        await ModifyDbEntities<ProportionalElectionResult>(_ => true, x => x.Published = true);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ZhMockedData.ContestIdBund),
            ExportTemplateIds =
            [
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlContestTemplates.ProportionalElectionResultsEch0252.Key,
                    SecureConnectTestDefaults.MockedTenantBund.Id)

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
