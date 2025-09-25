// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

// Test for VOTING-2833
public class PdfVoteEndResultMunicipalityExportTest : PdfExportBaseTest
{
    public PdfVoteEndResultMunicipalityExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => CreateService(
        tenantId: SecureConnectTestDefaults.MockedTenantGossau.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "Abst_Gossau_Gesamtergebnisse_20200831.pdf";

    protected override string TemplateKey => AusmittlungPdfVoteTemplates.EndResultProtocol.Key;

    protected override string SnapshotName => base.SnapshotName + "_municipality";

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var ccDetails = await db.ContestCountingCircleDetails
                .AsTracking()
                .FirstAsync(x => x.Id == AusmittlungUuidV5.BuildContestCountingCircleDetails(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidGossau, false));
            ccDetails.CountingMachine = CountingMachine.CalibratedScales;

            await db.SaveChangesAsync();
        });
    }

    protected override async Task<bool> SetToSubmissionOngoing()
    {
        var voteId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen);
        var results = await RunOnDb<List<VoteResult>>(db => db.VoteResults
            .Where(x => x.VoteId == voteId)
            .Include(x => x.Vote)
            .Include(x => x.CountingCircle)
            .ToListAsync());

        var ccDetailsEvents = results.Select(x => new ContestCountingCircleDetailsResetted
        {
            Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(x.Vote.ContestId, x.CountingCircle.BasisCountingCircleId, false).ToString(),
            ContestId = x.Vote.ContestId.ToString(),
            CountingCircleId = x.CountingCircle.BasisCountingCircleId.ToString(),
            EventInfo = GetMockedEventInfo(),
        }).ToArray();
        await TestEventPublisher.Publish(ccDetailsEvents);

        var voteEvents = results.Select(x => new VoteResultResetted
        {
            VoteResultId = x.Id.ToString(),
            EventInfo = GetMockedEventInfo(),
        }).ToArray();
        await TestEventPublisher.Publish(ccDetailsEvents.Length, voteEvents);
        return true;
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    TemplateKey,
                    SecureConnectTestDefaults.MockedTenantGossau.Id,
                    domainOfInfluenceId: DomainOfInfluenceMockedData.Gossau.Id)
                    .ToString(),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
