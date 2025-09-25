// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv;

public class CsvProportionalElectionCandidatesNumericalExportTest : CsvExportBaseTest
{
    public CsvProportionalElectionCandidatesNumericalExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => ErfassungElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Numerisches Kandidierendenverzeichnis.csv";

    [Fact]
    public async Task TestCsvWithCandidateCheckDigit()
    {
        await ModifyDbEntities<ProportionalElection>(
            x => x.Id == ProportionalElectionMockedData.StGallenProportionalElectionInContestBund.Id,
            x => x.CandidateCheckDigit = true);
        await TestCsvSnapshot(NewRequest(), NewRequestExpectedFileName, "with_candidate_check_digit");
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    protected override Task<bool> SetToSubmissionOngoing()
        => Task.FromResult(false); // Does not include any result data, submission ongoing does not make a difference

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleId = CountingCircleMockedData.GuidStGallen,
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungCsvProportionalElectionTemplates.CandidatesNumerical.Key,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: ProportionalElectionMockedData.StGallenProportionalElectionInContestBund.Id,
                    countingCircleId: CountingCircleMockedData.GuidStGallen),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
