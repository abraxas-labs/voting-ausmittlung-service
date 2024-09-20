// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfVoteCountingCircleEVotingResultExportTest : PdfExportBaseTest
{
    public PdfVoteCountingCircleEVotingResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => StGallenErfassungElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Abstimmungsprotokoll_inkl_E-Voting_Details_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfVoteTemplates.EVotingCountingCircleResultProtocol.Key;

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);

        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var countingCircleId = CountingCircleMockedData.GuidStGallen;
        await ModifyDbEntities<Contest>(
            x => x.Id == contestId,
            x => x.EVoting = true);

        await RunOnDb(async db =>
        {
            var contestDetails = await db.ContestCountingCircleDetails
                .AsTracking()
                .Include(x => x.VotingCards)
                .FirstAsync(x => x.ContestId == contestId && x.CountingCircle.BasisCountingCircleId == countingCircleId);

            contestDetails.EVoting = true;
            contestDetails.VotingCards.Add(new VotingCardResultDetail
            {
                Channel = VotingChannel.EVoting,
                Valid = true,
                DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                CountOfReceivedVotingCards = 25,
            });

            var results = await db.BallotResults
                .AsSplitQuery()
                .AsTracking()
                .Include(br => br.QuestionResults.OrderBy(bqr => bqr.Question.Number))
                .Include(br => br.TieBreakQuestionResults.OrderBy(tqr => tqr.Question.Number))
                .OrderBy(br => br.Ballot.Position)
                .Where(br => br.VoteResult.Vote.ContestId == contestId
                    && br.VoteResult.CountingCircle.BasisCountingCircleId == countingCircleId)
                .ToListAsync();

            foreach (var result in results)
            {
                result.CountOfVoters.EVotingReceivedBallots = 20;
                result.CountOfVoters.EVotingInvalidBallots = 2;
                result.CountOfVoters.EVotingBlankBallots = 3;
                result.CountOfVoters.EVotingAccountedBallots = 15;
                result.CountOfVoters.UpdateVoterParticipation(50);

                foreach (var bqr in result.QuestionResults)
                {
                    bqr.ConventionalSubTotal.TotalCountOfAnswerYes = 5;
                    bqr.ConventionalSubTotal.TotalCountOfAnswerNo = 4;
                    bqr.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1;
                    bqr.EVotingSubTotal.TotalCountOfAnswerYes = 3;
                    bqr.EVotingSubTotal.TotalCountOfAnswerNo = 2;
                    bqr.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1;
                }

                foreach (var tqr in result.TieBreakQuestionResults)
                {
                    tqr.ConventionalSubTotal.TotalCountOfAnswerQ1 = 4;
                    tqr.ConventionalSubTotal.TotalCountOfAnswerQ2 = 2;
                    tqr.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1;
                    tqr.EVotingSubTotal.TotalCountOfAnswerQ1 = 1;
                    tqr.EVotingSubTotal.TotalCountOfAnswerQ2 = 2;
                    tqr.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1;
                }
            }

            await db.SaveChangesAsync();
        });
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.GuidStGallen.ToString(),
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                        TemplateKey,
                        SecureConnectTestDefaults.MockedTenantStGallen.Id,
                        countingCircleId: CountingCircleMockedData.GuidStGallen)
                    .ToString(),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
