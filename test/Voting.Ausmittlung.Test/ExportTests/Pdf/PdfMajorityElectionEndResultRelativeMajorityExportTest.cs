// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionEndResultRelativeMajorityExportTest : PdfMajorityElectionEndResultExportTest
{
    public PdfMajorityElectionEndResultRelativeMajorityExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string SnapshotName
        => base.SnapshotName + "_relative_majority";

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped, MajorityElectionMandateAlgorithm.RelativeMajority);
    }
}
