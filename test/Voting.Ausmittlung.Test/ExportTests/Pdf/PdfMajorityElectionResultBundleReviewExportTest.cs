// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;
using PoliticalBusinessType = Abraxas.Voting.Ausmittlung.Services.V1.Models.PoliticalBusinessType;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionResultBundleReviewExportTest : PdfBundleReviewExportBaseTest
{
    public PdfMajorityElectionResultBundleReviewExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Bundkontrolle 1.pdf";

    protected override string TemplateKey => AusmittlungPdfMajorityElectionTemplates.ResultBundleReview.Key;

    [Fact]
    public async Task TestBundleWithoutCandidateCheckDigit()
    {
        await ModifyDbEntities<MajorityElectionResult>(
            x => x.Id == MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund,
            x => x.EntryParams!.CandidateCheckDigit = false);
        await TestPdfReport("_without_candidate_check_digit", NewRequest(), NewRequestExpectedFileName);
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
        await MajorityElectionResultBallotMockedData.Seed(RunScoped);

        await RunOnBundle<MajorityElectionResultBundleSubmissionFinished, MajorityElectionResultBundleAggregate>(
            MajorityElectionResultBundleMockedData.StGallenBundle1.Id,
            aggregate => aggregate.SubmissionFinished(ContestMockedData.GuidBundesurnengang));
    }

    protected override StartBundleReviewExportRequest NewRequest()
    {
        return new StartBundleReviewExportRequest
        {
            PoliticalBusinessResultBundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            PoliticalBusinessType = PoliticalBusinessType.MajorityElection,
        };
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);
}
