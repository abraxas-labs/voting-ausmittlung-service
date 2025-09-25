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

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfSecondaryMajorityElectionEndResultEVotingExportTest : PdfMajorityElectionEndResultExportBaseTest
{
    public PdfSecondaryMajorityElectionEndResultEVotingExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Majorz_Wahlprotokoll_EVoting_short2 de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfSecondaryMajorityElectionTemplates.EndResultEVotingProtocol.Key;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdBundesurnengang),
            x => x.EVoting = true);
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
