// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionEndResultTooManyCandidatesAbsoluteMajorityExportTest : PdfMajorityElectionEndResultExportTest
{
    public PdfMajorityElectionEndResultTooManyCandidatesAbsoluteMajorityExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string SnapshotName
        => base.SnapshotName + "_absolute_majority_too_many_candidates";

    [Fact]
    public async Task TestPdfWithRounding()
    {
        await ModifyDbEntities<MajorityElectionEndResult>(
            x => x.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x =>
            {
                x.Calculation.DecisiveVoteCount = 6605;
                x.Calculation.AbsoluteMajorityThreshold = 1651.25M;
                x.Calculation.AbsoluteMajority = 3303;
            });

        var request = NewRequest();
        await TestPdfReport("_with_rounding", TestClient, request);
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(
            RunScoped,
            primaryElectionNumberOfMandates: 1);
    }
}
