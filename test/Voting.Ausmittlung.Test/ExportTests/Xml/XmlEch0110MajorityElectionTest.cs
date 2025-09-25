// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0110_4_0;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Ech.Ech0110_4_0.Schemas;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0110MajorityElectionTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0110MajorityElectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0110_Majorzw de.xml";

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var details = await db.ContestCountingCircleDetails
                .AsTracking()
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == Guid.Parse(ContestMockedData.IdBundesurnengang) && x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidGossau);

            foreach (var subTotal in details.CountOfVotersInformationSubTotals.Where(st => st.VoterType == VoterType.Swiss && st.Sex == SexType.Male))
            {
                subTotal.CountOfVoters = 5000;
            }

            await db.SaveChangesAsync();
        });
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlMajorityElectionTemplates.Ech0110.Key,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: Guid.Parse(MajorityElectionEndResultMockedData.ElectionId)),
            },
        };
    }

    protected override XmlSchemaSet GetSchemaSet()
        => Ech0110Schemas.LoadEch0110Schemas();

    protected override void AssertTestDeliveryFlag(Delivery delivery) => delivery.DeliveryHeader.TestDeliveryFlag.Should().BeFalse();

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
