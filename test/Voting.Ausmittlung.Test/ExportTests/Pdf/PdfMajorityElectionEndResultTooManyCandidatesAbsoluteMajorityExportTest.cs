// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Test.MockedData;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionEndResultTooManyCandidatesAbsoluteMajorityExportTest : PdfMajorityElectionEndResultExportTest
{
    public PdfMajorityElectionEndResultTooManyCandidatesAbsoluteMajorityExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(
            RunScoped,
            primaryElectionNumberOfMandates: 1);
    }

    protected override string SnapshotName(GenerateResultExportsRequest request)
        => base.SnapshotName(request) + "_absolute_majority_too_many_candidates";
}
