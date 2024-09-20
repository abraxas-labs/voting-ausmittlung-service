// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;
using Ech0252_2_0;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Ech.Ech0252_2_0.Schemas;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0252ProportionalElectionInfoTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0252ProportionalElectionInfoTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0252_proportional-election-info-delivery_20290212.xml";

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionEndResultMockedData.Seed(RunScoped, ProportionalElectionMandateAlgorithm.HagenbachBischoff, 4);

        await RunOnDb(async db =>
        {
            var result = await db.ProportionalElectionResults
                .AsTracking()
                .SingleAsync(x => x.Id == ProportionalElectionEndResultMockedData.StGallenResultGuid);

            result.CountOfVoters = new()
            {
                ConventionalReceivedBallots = 900,
                ConventionalBlankBallots = 240,
                ConventionalInvalidBallots = 300,
                ConventionalAccountedBallots = 360,
            };

            result.TotalCountOfVoters = 1000;
            result.SubmissionDoneTimestamp = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            result.UpdateVoterParticipation();

            var lists = await db.ProportionalElectionLists
                .AsTracking()
                .AsSplitQuery()
                .Include(l => l.ProportionalElectionCandidates)
                .Include(l => l.Translations)
                .Where(l => l.ProportionalElection.Contest.Id == ContestMockedData.GuidBundesurnengang)
                .ToListAsync();

            foreach (var list in lists)
            {
                foreach (var listTranslation in list.Translations)
                {
                    if (string.IsNullOrEmpty(listTranslation.Description))
                    {
                        // List description is required in eCH-0252, and is always delivered in basis.
                        listTranslation.Description = "Mock List Description";
                    }
                }

                var candidates = list.ProportionalElectionCandidates
                    .OrderBy(c => c.Number)
                    .ToList();

                for (var i = 1; i <= candidates.Count; i++)
                {
                    candidates[i - 1].Number = i.ToString();
                }
            }

            await db.SaveChangesAsync();
        });

        // This list does not have any candidates, which is invalid in eCH-0252
        await RunOnDb(async db => await db.ProportionalElectionLists
            .Where(l => l.Id == Guid.Parse(ProportionalElectionMockedData.List2IdStGallenProportionalElectionInContestBund))
            .ExecuteDeleteAsync());

        await ModifyDbEntities<ProportionalElectionResult>(_ => true, x => x.Published = true);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds =
            [
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungXmlContestTemplates.ProportionalElectionInfosEch0252.Key,
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
