// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfVoteEndResultExportTest : PdfExportBaseTest
{
    public PdfVoteEndResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Abst_St. Gallen_Gesamtergebnisse_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfVoteTemplates.EndResultProtocol.Key;

    [Fact]
    public async Task TestPdfWithPartialResults()
    {
        await SeedCountingCircleDetails();

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

            haggenDetails.TotalCountOfVoters = 3210;
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
                },
                new CountOfVotersInformationSubTotal
                {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 1600,
                },
                new CountOfVotersInformationSubTotal
                {
                        Sex = SexType.Male,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 100,
                },
                new CountOfVotersInformationSubTotal
                {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 110,
                },
            };

            await db.SaveChangesAsync();
        });
    }
}
