// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionEndResultEVotingExportTest : PdfExportBaseTest
{
    public PdfMajorityElectionEndResultEVotingExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Majorz_Wahlprotokoll_EVoting_Majorzw de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfMajorityElectionTemplates.EndResultEVotingProtocol.Key;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdBundesurnengang),
            x => x.EVoting = true);
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);
    }

    protected override async Task<bool> SetToSubmissionOngoing()
    {
        var majorityElectionId = Guid.Parse(MajorityElectionEndResultMockedData.ElectionId);
        var results = await RunOnDb<List<MajorityElectionResult>>(db => db.MajorityElectionResults
            .Include(x => x.MajorityElection)
            .Include(x => x.CountingCircle)
            .Where(x => x.MajorityElectionId == majorityElectionId)
            .ToListAsync());

        var ccDetailsEvents = results.Select(x => new ContestCountingCircleDetailsResetted
        {
            Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(x.MajorityElection.ContestId, x.CountingCircle.BasisCountingCircleId, false).ToString(),
            ContestId = x.MajorityElection.ContestId.ToString(),
            CountingCircleId = x.CountingCircle.BasisCountingCircleId.ToString(),
            EventInfo = GetMockedEventInfo(),
        }).ToArray();

        await TestEventPublisher.Publish(ccDetailsEvents);

        var meEvents = results.Select(x => new MajorityElectionResultResetted
        {
            ElectionResultId = x.Id.ToString(),
            EventInfo = GetMockedEventInfo(),
        }).ToArray();
        await TestEventPublisher.Publish(ccDetailsEvents.Length, meEvents);
        return true;
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    TemplateKey,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: Guid.Parse(MajorityElectionEndResultMockedData.ElectionId))
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
