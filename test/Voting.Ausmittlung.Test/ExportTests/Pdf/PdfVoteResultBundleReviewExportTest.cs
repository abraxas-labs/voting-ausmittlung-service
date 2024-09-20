// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using PoliticalBusinessType = Abraxas.Voting.Ausmittlung.Services.V1.Models.PoliticalBusinessType;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfVoteResultBundleReviewExportTest : PdfBundleReviewExportBaseTest
{
    public PdfVoteResultBundleReviewExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => CreateServiceWithTenant(
        SecureConnectTestDefaults.MockedTenantGossau.Id,
        RolesMockedData.ErfassungElectionAdmin);

    protected override string NewRequestExpectedFileName => "Bundkontrolle 1.pdf";

    protected override string TemplateKey => AusmittlungPdfVoteTemplates.ResultBundleReview.Key;

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);
        await VoteResultBundleMockedData.Seed(RunScoped);
        await VoteResultBallotMockedData.Seed(RunScoped);

        await RunOnBundle<VoteResultBundleSubmissionFinished, VoteResultBundleAggregate>(
            VoteResultBundleMockedData.GossauBundle1.Id,
            aggregate => aggregate.SubmissionFinished(ContestMockedData.GuidStGallenEvoting));
    }

    protected override StartBundleReviewExportRequest NewRequest()
    {
        return new StartBundleReviewExportRequest()
        {
            PoliticalBusinessResultBundleId = VoteResultBundleMockedData.IdGossauBundle1,
            PoliticalBusinessType = PoliticalBusinessType.Vote,
        };
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);
}
