// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Csv;

public class CsvProportionalElectionUnionVoterParticipationExportTest : CsvExportBaseTest
{
    public CsvProportionalElectionUnionVoterParticipationExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Wahlbeteiligung.csv";

    protected override Task SeedData()
        => ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);

    protected override GenerateResultExportsRequest NewRequest()
        => new()
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ResultExportRequests =
            {
                new GenerateResultExportRequest
                {
                    Key = AusmittlungCsvProportionalElectionUnionTemplates.VoterParticipation.Key,
                    PoliticalBusinessUnionId = Guid.Parse(ProportionalElectionUnionEndResultMockedData.UnionId),
                },
            },
        };

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
