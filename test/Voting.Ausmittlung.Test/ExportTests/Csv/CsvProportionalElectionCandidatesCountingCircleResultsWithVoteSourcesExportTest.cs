// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Csv;

public class CsvProportionalElectionCandidatesCountingCircleResultsWithVoteSourcesExportTest : CsvExportBaseTest
{
    public CsvProportionalElectionCandidatesCountingCircleResultsWithVoteSourcesExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantUzwil.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "Kandidatenstimmen Gesamttotal je Liste - unveränderte und veränderte Wahlzettel Uzwil.csv";

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
                    SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    politicalBusinessId: Guid.Parse(ProportionalElectionUnionEndResultMockedData.UzwilElectionId)),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.ErfassungCreator;
    }
}
