// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionEndResultDetailExportTest : PdfExportBaseTest
{
    public PdfMajorityElectionEndResultDetailExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Majorz_Detailergebnisse_Majorzw de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfMajorityElectionTemplates.EndResultDetailProtocol.Key;

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

    [Fact]
    public async Task TestPdfWithHideLowerDoisFlag()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.SnapshotContestId == ContestMockedData.GuidBundesurnengang && x.BasisDomainOfInfluenceId ==
                DomainOfInfluenceMockedData.StGallenStadt.BasisDomainOfInfluenceId,
            x =>
            {
                x.HideLowerDomainOfInfluencesInReports = true;
                x.SecureConnectId = "abc";
            });

        var request = NewRequest();
        await TestPdfReport("_hide_lower_dois", TestClient, request);
    }

    [Fact]
    public async Task TestPdfWithHideLowerDoisFlagAndSameTenant()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.SnapshotContestId == ContestMockedData.GuidBundesurnengang && x.BasisDomainOfInfluenceId ==
                DomainOfInfluenceMockedData.StGallenStadt.BasisDomainOfInfluenceId,
            x => x.HideLowerDomainOfInfluencesInReports = true);

        var request = NewRequest();
        await TestPdfReport("_hide_lower_dois_same_tenant", TestClient, request);
    }

    [Fact]
    public async Task TestPdfWithEmptyVoteCountDisabled()
    {
        var primaryElectionId = MajorityElectionEndResultMockedData.ElectionGuid;

        await ModifyDbEntities<MajorityElection>(
            x => x.Id == MajorityElectionEndResultMockedData.ElectionGuid,
            x => x.NumberOfMandates = 1);

        await RunScoped<IDbRepository<DataContext, SecondaryMajorityElection>>(repo =>
            repo.DeleteRangeByKey(new[] { Guid.Parse(MajorityElectionEndResultMockedData.SecondaryElectionId), Guid.Parse(MajorityElectionEndResultMockedData.SecondaryElectionId2) }));

        var request = NewRequest();
        await TestPdfReport("_empty_vote_count_disabled", TestClient, request);
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);

        await ModifyDbEntities<MajorityElectionResult>(_ => true, x => x.State = CountingCircleResultState.AuditedTentatively);
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
                    politicalBusinessId: Guid.Parse(MajorityElectionEndResultMockedData.ElectionId))
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
