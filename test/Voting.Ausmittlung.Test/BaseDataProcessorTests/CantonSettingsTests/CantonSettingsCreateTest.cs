// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.CantonSettingsTests;

public class CantonSettingsCreateTest : BaseDataProcessorTest
{
    public CantonSettingsCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestCreated()
    {
        await ContestMockedData.Seed(RunScoped);

        _ = await RunOnDb(db =>
        {
            db.DomainOfInfluences.Add(new DomainOfInfluence
            {
                Id = Guid.Parse("e84a3f1e-c2ea-422c-904e-130b822aad64"),
                Canton = DomainOfInfluenceCanton.Tg,
            });
            return db.SaveChangesAsync();
        });

        var newId = Guid.Parse("2d203a3c-40ba-4b53-a57e-38909d71390c");

        await TestEventPublisher.Publish(
            new CantonSettingsCreated
            {
                CantonSettings = new CantonSettingsEventData
                {
                    Id = newId.ToString(),
                    Canton = SharedProto.DomainOfInfluenceCanton.Tg,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                    AuthorityName = "Staatskanzlei Thurgau",
                    ProportionalElectionMandateAlgorithms =
                    {
                            SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                    },
                    MajorityElectionAbsoluteMajorityAlgorithm = SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates,
                    MajorityElectionInvalidVotes = true,
                    SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.OnEveryCountingCircle,
                    SwissAbroadVotingRightDomainOfInfluenceTypes =
                    {
                            SharedProto.DomainOfInfluenceType.Ch,
                            SharedProto.DomainOfInfluenceType.An,
                    },
                },
            });

        var result = await RunOnDb(db => db.CantonSettings.FirstOrDefaultAsync(u => u.Id == newId));
        result.MatchSnapshot("cantonSettings");

        var affectedDois = await RunOnDb(db => db.DomainOfInfluences.Where(doi => doi.Canton == DomainOfInfluenceCanton.Tg).ToListAsync());
        affectedDois.MatchSnapshot("affectedDomainOfInfluences");
    }
}
