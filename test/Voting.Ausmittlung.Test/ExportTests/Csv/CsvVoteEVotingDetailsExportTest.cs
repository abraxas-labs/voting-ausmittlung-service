// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Csv;

public class CsvVoteEVotingDetailsExportTest : CsvExportBaseTest
{
    public CsvVoteEVotingDetailsExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => BundMonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Abstimmungsergebnisse_E-Voting.csv";

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);

        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var testCountingCircleIds = new List<Guid>
        {
            CountingCircleMockedData.GuidStGallenAuslandschweizer,
            CountingCircleMockedData.GuidGossau,
            CountingCircleMockedData.GuidUzwil,
        };

        await ModifyDbEntities<Contest>(
            x => x.Id == contestId,
            x => x.EVoting = true);

        for (var i = 0; i < testCountingCircleIds.Count; i++)
        {
            await ModifiyEVotingResults(contestId, testCountingCircleIds[i], i + 1);
        }
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungCsvVoteTemplates.EVotingDetails.Key,
                    CountingCircleMockedData.Bund.ResponsibleAuthority.SecureConnectId),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.ErfassungCreator;
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

            var voteResults = await db.VoteResults
                .AsTracking()
                .Where(vr => vr.Vote.ContestId == contestId && vr.CountingCircle.BasisCountingCircleId == countingCircleId)
                .ToListAsync();

            var ballotResults = await db.BallotResults
                .AsSplitQuery()
                .AsTracking()
                .Include(br => br.QuestionResults.OrderBy(bqr => bqr.Question.Number))
                .Include(br => br.TieBreakQuestionResults.OrderBy(tqr => tqr.Question.Number))
                .OrderBy(br => br.Ballot.Position)
                .Where(br => br.VoteResult.Vote.ContestId == contestId
                    && br.VoteResult.CountingCircle.BasisCountingCircleId == countingCircleId)
                .ToListAsync();

            foreach (var voteResult in voteResults)
            {
                voteResult.TotalSentEVotingVotingCards = 5 * modifier;
            }

            foreach (var ballotResult in ballotResults)
            {
                ballotResult.CountOfVoters.EVotingReceivedBallots = 20 * modifier;
                ballotResult.CountOfVoters.EVotingInvalidBallots = 2 * modifier;
                ballotResult.CountOfVoters.EVotingBlankBallots = 3 * modifier;
                ballotResult.CountOfVoters.EVotingAccountedBallots = 15 * modifier;
                ballotResult.CountOfVoters.UpdateVoterParticipation(50 * modifier);

                foreach (var bqr in ballotResult.QuestionResults)
                {
                    bqr.ConventionalSubTotal.TotalCountOfAnswerYes = 5 * modifier;
                    bqr.ConventionalSubTotal.TotalCountOfAnswerNo = 4 * modifier;
                    bqr.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                    bqr.EVotingSubTotal.TotalCountOfAnswerYes = 3 * modifier;
                    bqr.EVotingSubTotal.TotalCountOfAnswerNo = 2 * modifier;
                    bqr.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                }

                foreach (var tqr in ballotResult.TieBreakQuestionResults)
                {
                    tqr.ConventionalSubTotal.TotalCountOfAnswerQ1 = 4 * modifier;
                    tqr.ConventionalSubTotal.TotalCountOfAnswerQ2 = 2 * modifier;
                    tqr.ConventionalSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                    tqr.EVotingSubTotal.TotalCountOfAnswerQ1 = 1 * modifier;
                    tqr.EVotingSubTotal.TotalCountOfAnswerQ2 = 2 * modifier;
                    tqr.EVotingSubTotal.TotalCountOfAnswerUnspecified = 1 * modifier;
                }
            }

            await db.SaveChangesAsync();
        });
    }
}
