// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfSecondaryMajorityElectionEndResultDetailWithoutEmptyAndInvalidVotesExportTest : PdfMajorityElectionEndResultExportBaseTest
{
    public PdfSecondaryMajorityElectionEndResultDetailWithoutEmptyAndInvalidVotesExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Majorz_Detailergebnisse_exkl_short2 de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfSecondaryMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key;

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
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);

        var electionId = Guid.Parse(MajorityElectionEndResultMockedData.ElectionId);
        await ModifyDbEntities<MajorityElectionResult>(
            x => x.MajorityElectionId == electionId,
            x => x.State = CountingCircleResultState.AuditedTentatively);
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.MajorityElectionInvalidVotes = true,
            true);
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
                    politicalBusinessId: Guid.Parse(MajorityElectionEndResultMockedData.SecondaryElectionId2))
                    .ToString(),
            },
        };
    }
}
