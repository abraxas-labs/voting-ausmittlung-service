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

public class PdfVoteEVotingDetailsResultExportTest : PdfExportBaseTest
{
    private static readonly List<Guid> _testCountingCircleIds =
    [
        CountingCircleMockedData.GuidStGallenAuslandschweizer,
        CountingCircleMockedData.GuidGossau,
        CountingCircleMockedData.GuidUzwil
    ];

    public PdfVoteEVotingDetailsResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => CreateService(
        tenantId: SecureConnectTestDefaults.MockedTenantBund.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "Abstimmungsprotokoll_E-Voting_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfVoteTemplates.EVotingDetailsResultProtocol.Key;

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);

        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        await ModifyDbEntities<Contest>(
            x => x.Id == contestId,
            x => x.EVoting = true);

        for (var i = 0; i < _testCountingCircleIds.Count; i++)
        {
            await ModifiyEVotingResults(contestId, _testCountingCircleIds[i], i + 1);
        }
    }

    protected override async Task<bool> SetToSubmissionOngoing()
    {
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        await ModifyDbEntities<VoteResult>(
            x => x.Vote.ContestId == contestId
                 && _testCountingCircleIds.Contains(x.CountingCircle.BasisCountingCircleId),
            x => x.State = CountingCircleResultState.SubmissionOngoing);
        return true;
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(TemplateKey, SecureConnectTestDefaults.MockedTenantBund.Id).ToString(),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private async Task ModifiyEVotingResults(Guid contestId, Guid countingCircleId, int modifier)
    {
        await RunOnDb(async db =>
        {
            var contestDetails = await db.ContestCountingCircleDetails
                .AsTracking()
                .Include(x => x.VotingCards)
                .FirstAsync(x => x.ContestId == contestId && x.CountingCircle.BasisCountingCircleId == countingCircleId);

            contestDetails.EVoting = true;
            var eVotingVotingCard = contestDetails.VotingCards
                .FirstOrDefault(vc => vc.Channel == VotingChannel.EVoting && vc.DomainOfInfluenceType == DomainOfInfluenceType.Ch);
            if (eVotingVotingCard == null)
            {
                contestDetails.VotingCards.Add(new VotingCardResultDetail
                {
                    Channel = VotingChannel.EVoting,
                    Valid = true,
                    DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    CountOfReceivedVotingCards = 10 * modifier,
                });
            }
            else
            {
                eVotingVotingCard.CountOfReceivedVotingCards = 10 * modifier;
            }

            var results = await db.BallotResults
                .AsSplitQuery()
                .AsTracking()
                .Include(x => x.VoteResult)
                .Include(x => x.Ballot.EndResult)
                .Include(br => br.QuestionResults.OrderBy(bqr => bqr.Question.Number))
                .ThenInclude(x => x.Question.EndResult)
                .Include(br => br.TieBreakQuestionResults.OrderBy(tqr => tqr.Question.Number))
                .ThenInclude(x => x.Question.EndResult)
                .OrderBy(br => br.Ballot.Position)
                .Where(br => br.VoteResult.Vote.ContestId == contestId
                    && br.VoteResult.CountingCircle.BasisCountingCircleId == countingCircleId)
                .ToListAsync();

            foreach (var result in results)
            {
                result.VoteResult.State = CountingCircleResultState.SubmissionDone;
                result.CountOfVoters.EVotingSubTotal.ReceivedBallots = 20 * modifier;
                result.CountOfVoters.EVotingSubTotal.InvalidBallots = 2 * modifier;
                result.CountOfVoters.EVotingSubTotal.BlankBallots = 3 * modifier;
                result.CountOfVoters.EVotingSubTotal.AccountedBallots = 15 * modifier;
                result.CountOfVoters.UpdateVoterParticipation(50 * modifier);

                result.Ballot.EndResult!.CountOfVoters.EVotingSubTotal.ReceivedBallots = 20 * modifier;
                result.Ballot.EndResult.CountOfVoters.EVotingSubTotal.InvalidBallots = 2 * modifier;
                result.Ballot.EndResult.CountOfVoters.EVotingSubTotal.BlankBallots = 3 * modifier;
                result.Ballot.EndResult.CountOfVoters.EVotingSubTotal.AccountedBallots = 15 * modifier;
                result.Ballot.EndResult.CountOfVoters.UpdateVoterParticipation(50 * modifier);

                foreach (var bqr in result.QuestionResults)
                {
                    bqr.ConventionalSubTotal.TotalCountOfAnswerYes = 5 * modifier;
                    bqr.ConventionalSubTotal.TotalCountOfAnswerNo = 4 * modifier;
                    bqr.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                    bqr.EVotingSubTotal.TotalCountOfAnswerYes = 3 * modifier;
                    bqr.EVotingSubTotal.TotalCountOfAnswerNo = 2 * modifier;
                    bqr.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;

                    bqr.Question.EndResult!.ConventionalSubTotal.TotalCountOfAnswerYes = 5 * modifier;
                    bqr.Question.EndResult.ConventionalSubTotal.TotalCountOfAnswerNo = 4 * modifier;
                    bqr.Question.EndResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                    bqr.Question.EndResult.EVotingSubTotal.TotalCountOfAnswerYes = 3 * modifier;
                    bqr.Question.EndResult.EVotingSubTotal.TotalCountOfAnswerNo = 2 * modifier;
                    bqr.Question.EndResult.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                }

                foreach (var tqr in result.TieBreakQuestionResults)
                {
                    tqr.ConventionalSubTotal.TotalCountOfAnswerQ1 = 4 * modifier;
                    tqr.ConventionalSubTotal.TotalCountOfAnswerQ2 = 2 * modifier;
                    tqr.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                    tqr.EVotingSubTotal.TotalCountOfAnswerQ1 = 1 * modifier;
                    tqr.EVotingSubTotal.TotalCountOfAnswerQ2 = 2 * modifier;
                    tqr.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;

                    tqr.Question.EndResult!.ConventionalSubTotal.TotalCountOfAnswerQ1 = 4 * modifier;
                    tqr.Question.EndResult.ConventionalSubTotal.TotalCountOfAnswerQ2 = 2 * modifier;
                    tqr.Question.EndResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                    tqr.Question.EndResult.EVotingSubTotal.TotalCountOfAnswerQ1 = 1 * modifier;
                    tqr.Question.EndResult.EVotingSubTotal.TotalCountOfAnswerQ2 = 2 * modifier;
                    tqr.Question.EndResult.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                }
            }

            await db.SaveChangesAsync();
        });
    }
}
