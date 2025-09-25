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
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfVoteEndResultExportTest : PdfExportBaseTest
{
    public PdfVoteEndResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Abst_St. Gallen_Gesamtergebnisse_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfVoteTemplates.EndResultProtocol.Key;

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);

        var voteId = Guid.Parse(VoteMockedData.IdStGallenVoteInContestBund);

        await ModifyDbEntities<VoteResult>(
            x => x.VoteId == voteId,
            x => x.State = CountingCircleResultState.SubmissionDone);

        var voteResultIds = await RunOnDb(db => db.VoteResults
            .Where(v => v.VoteId == voteId)
            .Select(v => v.Id)
            .ToListAsync());

        foreach (var voteResultId in voteResultIds)
        {
            await RunScoped<VoteEndResultBuilder>(b => b.AdjustVoteEndResult(voteResultId, false));
        }

        await ModifyDbEntities<SimpleCountingCircleResult>(
            x => x.PoliticalBusinessId == voteId,
            x => x.State = CountingCircleResultState.SubmissionDone);
    }

    protected override async Task<bool> SetToSubmissionOngoing()
    {
        var voteId = Guid.Parse(VoteMockedData.IdStGallenVoteInContestBund);

        var results = await RunOnDb(db => db.VoteResults
            .Include(x => x.Vote)
            .Include(x => x.CountingCircle)
            .Where(x => x.VoteId == voteId && x.State >= CountingCircleResultState.SubmissionDone)
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
            ContestId = ContestMockedData.IdBundesurnengang,
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    TemplateKey,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    domainOfInfluenceId: DomainOfInfluenceMockedData.StGallen.Id)
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

    private async Task SeedCountingCircleDetails()
    {
        await RunOnDb(async db =>
        {
            var haggenDetails = await db.ContestCountingCircleDetails
                .AsTracking()
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang
                    && x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidStGallenHaggen);

            haggenDetails.VotingCards = new List<VotingCardResultDetail>
            {
                new VotingCardResultDetail
                {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new VotingCardResultDetail
                {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 400,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new VotingCardResultDetail
                {
                        Channel = VotingChannel.Paper,
                        Valid = true,
                        CountOfReceivedVotingCards = 200,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new VotingCardResultDetail
                {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new VotingCardResultDetail
                {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new VotingCardResultDetail
                {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Mu,
                },
            };

            haggenDetails.CountOfVotersInformationSubTotals = new List<CountOfVotersInformationSubTotal>
            {
                new CountOfVotersInformationSubTotal
                {
                        Sex = SexType.Female,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 1400,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new CountOfVotersInformationSubTotal
                {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 1600,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new CountOfVotersInformationSubTotal
                {
                        Sex = SexType.Male,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 100,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new CountOfVotersInformationSubTotal
                {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 110,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                },
                new CountOfVotersInformationSubTotal
                {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 1,
                        DomainOfInfluenceType = DomainOfInfluenceType.Mu,
                },
            };

            await db.SaveChangesAsync();
        });
    }
}
