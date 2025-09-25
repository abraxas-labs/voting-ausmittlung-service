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
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public class XmlEch0252ProportionalElectionResultTest : XmlExportBaseTest<Delivery>
{
    public XmlEch0252ProportionalElectionResultTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "eCH-0252_proportional-election-result-delivery_20290212.xml";

    [Fact]
    public async Task TestWithoutPublished()
    {
        await ModifyDbEntities<ProportionalElectionResult>(x => x.Id == ProportionalElectionEndResultMockedData.StGallenResultGuid, x => x.Published = false);
        await TestXmlWithSnapshot("WithoutPublished");
    }

    [Fact]
    public async Task TestEVoting()
    {
        await ModifyDbEntities<Contest>(x => x.Id == ContestMockedData.GuidBundesurnengang, x => x.EVoting = true);
        await TestXmlWithSnapshot("EVoting");
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionEndResultMockedData.Seed(RunScoped, ProportionalElectionMandateAlgorithm.HagenbachBischoff, 4);

        await RunOnDb(async db =>
        {
            var result = await db.ProportionalElectionResults
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ListResults)
                .ThenInclude(x => x.CandidateResults)
                .ThenInclude(x => x.VoteSources)
                .SingleAsync(x => x.Id == ProportionalElectionEndResultMockedData.StGallenResultGuid);

            result.CountOfVoters = new()
            {
                ConventionalSubTotal = new PoliticalBusinessCountOfVotersNullableSubTotal
                {
                    ReceivedBallots = 900,
                    BlankBallots = 240,
                    InvalidBallots = 300,
                    AccountedBallots = 360,
                },
            };

            result.TotalCountOfVoters = 1000;
            result.SubmissionDoneTimestamp = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            result.UpdateVoterParticipation();

            var listIds = result.ListResults
                .OrderBy(x => x.ListId)
                .Select(x => x.ListId)
                .ToList();
            var candidates = result.ListResults.SelectMany(x => x.CandidateResults);
            foreach (var candidate in candidates)
            {
                // These should not be included in the XML for this report
                // Add them anyway to verify that they do not show up
                candidate.VoteSources.Add(new ProportionalElectionCandidateVoteSourceResult
                {
                    ConventionalVoteCount = 3,
                });

                foreach (var (listId, index) in listIds.Select((x, i) => (x, i)))
                {
                    candidate.VoteSources.Add(new ProportionalElectionCandidateVoteSourceResult
                    {
                        ConventionalVoteCount = index + 1,
                        EVotingVoteCount = index,
                        ListId = listId,
                    });
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
                    AusmittlungXmlContestTemplates.ProportionalElectionResultsEch0252.Key,
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
