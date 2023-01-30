// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using DomainOfInfluenceType = Voting.Lib.VotingExports.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfVoteDomainOfInfluenceTemporaryResultExportTest : PdfExportBaseTest<GenerateResultExportsRequest>
{
    public PdfVoteDomainOfInfluenceTemporaryResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Abst_CT_provisorischeErgebnisse_Abst SG de_20200110.pdf";

    protected override string ContestId => ContestMockedData.IdBundesurnengang;

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);

        await ModifyDbEntities<VoteResult>(_ => true, x => x.State = CountingCircleResultState.AuditedTentatively);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestId),
            ResultExportRequests =
            {
                new GenerateResultExportRequest
                {
                    Key = AusmittlungPdfVoteTemplates.TemporaryEndResultDomainOfInfluencesProtocol.Key,
                    DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    PoliticalBusinessIds =
                    {
                        Guid.Parse(VoteEndResultMockedData.VoteId),
                    },
                },
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
