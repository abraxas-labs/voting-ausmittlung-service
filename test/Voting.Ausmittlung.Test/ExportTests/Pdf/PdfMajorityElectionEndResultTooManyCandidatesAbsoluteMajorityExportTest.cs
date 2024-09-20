// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Voting.Ausmittlung.Test.MockedData;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionEndResultTooManyCandidatesAbsoluteMajorityExportTest : PdfMajorityElectionEndResultExportTest
{
    public PdfMajorityElectionEndResultTooManyCandidatesAbsoluteMajorityExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string SnapshotName
        => base.SnapshotName + "_absolute_majority_too_many_candidates";

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(
            RunScoped,
            primaryElectionNumberOfMandates: 1);
    }
}
