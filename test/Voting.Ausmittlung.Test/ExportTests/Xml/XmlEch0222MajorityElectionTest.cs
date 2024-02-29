// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0222_1_0;
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
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlMajorityElectionTemplates.Ech0222.Key,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund)),
            },
        };
    }

    protected override XmlSchemaSet GetSchemaSet()
        => Ech0222Schemas.LoadEch0222Schemas();

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
