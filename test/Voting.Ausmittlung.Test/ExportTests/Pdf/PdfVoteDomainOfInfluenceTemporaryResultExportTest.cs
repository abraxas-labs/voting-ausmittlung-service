﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfVoteDomainOfInfluenceTemporaryResultExportTest : PdfExportBaseTest
{
    public PdfVoteDomainOfInfluenceTemporaryResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Abst_Kant_provisorischeErgebnisse_Abst SG de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfVoteTemplates.TemporaryEndResultDomainOfInfluencesProtocol.Key;

    [Fact]
    public async Task TestPdfWithPartialResults()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.SnapshotContestId == ContestMockedData.GuidBundesurnengang && x.BasisDomainOfInfluenceId ==
                DomainOfInfluenceMockedData.StGallenStadt.BasisDomainOfInfluenceId,
            x => x.ViewCountingCirclePartialResults = true);
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.SnapshotContestId == ContestMockedData.GuidBundesurnengang && x.BasisDomainOfInfluenceId ==
                DomainOfInfluenceMockedData.StGallen.BasisDomainOfInfluenceId,
            x => x.SecureConnectId = "random_partial_result");

        var request = NewRequest();
        await TestPdfReport("_with_partial_result", TestClient, request);
    }

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);

        await ModifyDbEntities<VoteResult>(_ => true, x => x.State = CountingCircleResultState.AuditedTentatively);
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
                    politicalBusinessId: Guid.Parse(VoteEndResultMockedData.VoteId))
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
