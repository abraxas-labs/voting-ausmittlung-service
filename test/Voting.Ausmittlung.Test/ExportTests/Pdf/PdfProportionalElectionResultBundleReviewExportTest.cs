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

public class PdfProportionalElectionResultBundleReviewExportTest : PdfBundleReviewExportBaseTest
{
    public PdfProportionalElectionResultBundleReviewExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Bundkontrolle 2.pdf";

    protected override string TemplateKey => AusmittlungPdfProportionalElectionTemplates.ResultBundleReview.Key;

    [Fact]
    public async Task TestBundleWithoutParty()
    {
        var request = NewRequest();
        request.PoliticalBusinessResultBundleId = ProportionalElectionResultBundleMockedData.IdUzwilBundle1;
        await TestPdfReport("_without_party", request, "Bundkontrolle 1.pdf");
    }

    [Fact]
    public async Task TestBundleWithCandidateCheckDigit()
    {
        await ModifyDbEntities<ProportionalElectionResult>(
            x => x.Id == ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
            x => x.EntryParams.CandidateCheckDigit = true);
        await TestPdfReport("_with_candidate_check_digit", NewRequest(), "Bundkontrolle 2.pdf");
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
        await ProportionalElectionResultBallotMockedData.Seed(RunScoped);

        await RunOnBundle<ProportionalElectionResultBundleSubmissionFinished, ProportionalElectionResultBundleAggregate>(
            ProportionalElectionResultBundleMockedData.UzwilBundle1NoList.Id,
            aggregate => aggregate.SubmissionFinished(ContestMockedData.GuidUzwilEvoting));

        await RunOnBundle<ProportionalElectionResultBundleSubmissionFinished, ProportionalElectionResultBundleAggregate>(
            ProportionalElectionResultBundleMockedData.UzwilBundle2.Id,
            aggregate => aggregate.SubmissionFinished(ContestMockedData.GuidUzwilEvoting));
    }

    protected override StartBundleReviewExportRequest NewRequest()
    {
        return new StartBundleReviewExportRequest
        {
            PoliticalBusinessResultBundleId = ProportionalElectionResultBundleMockedData.IdUzwilBundle2,
            PoliticalBusinessType = PoliticalBusinessType.ProportionalElection,
        };
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);
}
