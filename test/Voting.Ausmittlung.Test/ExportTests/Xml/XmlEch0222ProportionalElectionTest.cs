// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0222_1_0;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Ech.Ech0222_1_0.Schemas;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0222ProportionalElectionTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0222ProportionalElectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantUzwil.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "eCH-0222_Pw Uzwil de.xml";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SetBundlesReviewed();
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
        await ProportionalElectionResultBallotMockedData.Seed(RunScoped);
        await ProportionalElectionUnmodifiedListResultMockedData.Seed(RunScoped);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdUzwilEVoting),
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlProportionalElectionTemplates.Ech0222.Key,
                    SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    politicalBusinessId: Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds)),
            },
        };
    }

    protected override XmlSchemaSet GetSchemaSet()
        => Ech0222Schemas.LoadEch0222Schemas();

    protected override void AssertTestDeliveryFlag(Delivery delivery) => delivery.DeliveryHeader.TestDeliveryFlag.Should().BeFalse();

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
            var bundles = await db.ProportionalElectionBundles
                .AsTracking()
                .Where(x => x.ElectionResultId == ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil)
                .ToListAsync();

            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            await db.SaveChangesAsync();
        });
    }
}
