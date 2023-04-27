// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using eCH_0222_1_0.Standard;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Ech.Schemas;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0222VoteTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0222VoteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantGossau.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "eCH-0222_Abst Gossau de.xml";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SetBundlesReviewed();
    }

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteResultBundleMockedData.Seed(RunScoped);
        await VoteResultBallotMockedData.Seed(RunScoped);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlVoteTemplates.Ech0222.Key,
                    SecureConnectTestDefaults.MockedTenantGossau.Id,
                    politicalBusinessId: Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)),
            },
        };
    }

    protected override XmlSchemaSet GetSchemaSet()
        => Ech0222SchemaLoader.LoadEch0222Schemas();

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
            var bundles = await db.VoteResultBundles
                .AsTracking()
                .Where(x => x.BallotResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult))
                .ToListAsync();

            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            await db.SaveChangesAsync();
        });
    }
}
