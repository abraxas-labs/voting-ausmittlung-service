// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

public class PdfVoteResultExportTest : PdfExportBaseTest
{
    public PdfVoteResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => CreateService(
        tenantId: SecureConnectTestDefaults.MockedTenantUzwil.Id,
        roles: RolesMockedData.ErfassungElectionAdmin);

    protected override string NewRequestExpectedFileName => "Abstimmungsprotokoll_Eidg_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfVoteTemplates.ResultProtocol.Key;

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var result = await db.BallotResults
                .AsSplitQuery()
                .AsTracking()
                .Include(br => br.QuestionResults.OrderBy(bqr => bqr.Question.Number))
                .Include(br => br.TieBreakQuestionResults.OrderBy(tqr => tqr.Question.Number))
                .OrderBy(br => br.Ballot.Position)
                .FirstAsync(br => br.VoteResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && br.VoteResult.VoteId == Guid.Parse(VoteMockedData.IdBundVote2InContestBund));

            var bqr = result.QuestionResults.First();
            bqr.ConventionalSubTotal.TotalCountOfAnswerYes = 15;
            bqr.ConventionalSubTotal.TotalCountOfAnswerNo = 10;
            bqr.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1;
            bqr.EVotingSubTotal.TotalCountOfAnswerYes = 3;
            bqr.EVotingSubTotal.TotalCountOfAnswerNo = 1;
            bqr.EVotingSubTotal.TotalCountOfAnswerUnspecified = 0;

            var tqr = result.TieBreakQuestionResults.First();
            tqr.ConventionalSubTotal.TotalCountOfAnswerQ1 = 9;
            tqr.ConventionalSubTotal.TotalCountOfAnswerQ2 = 0;
            tqr.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1;
            tqr.EVotingSubTotal.TotalCountOfAnswerQ1 = 0;
            tqr.EVotingSubTotal.TotalCountOfAnswerQ2 = 2;
            tqr.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1;
            await db.SaveChangesAsync();
        });

        await ModifyDbEntities<Vote>(v => v.Id == VoteMockedData.BundVoteInContestBund.Id, v => v.ReportDomainOfInfluenceLevel = 1);
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.GuidUzwil.ToString(),
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    TemplateKey,
                    SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    domainOfInfluenceType: Data.Models.DomainOfInfluenceType.Ch,
                    countingCircleId: CountingCircleMockedData.GuidUzwil)
                    .ToString(),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
