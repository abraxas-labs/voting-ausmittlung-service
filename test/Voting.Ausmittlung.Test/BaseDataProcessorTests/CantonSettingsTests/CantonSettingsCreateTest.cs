// (c) Copyright by Abraxas Informatik AG
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
            var doiId = Guid.Parse("e84a3f1e-c2ea-422c-904e-130b822aad64");
            var contestId = Guid.Parse("b5efaeac-2bed-4dd6-987d-c52bd7482dfb");
            db.DomainOfInfluences.Add(new DomainOfInfluence
            {
                Id = doiId,
                Canton = DomainOfInfluenceCanton.Tg,
            });
            db.Contests.Add(new Contest
            {
                Id = contestId,
                DomainOfInfluenceId = doiId,
            });
            db.ContestTranslations.Add(new ContestTranslation
            {
                Id = Guid.Parse("e25ca312-413d-4783-a501-f86c786ca3a8"),
                ContestId = contestId,
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
                    ProtocolCountingCircleSortType = SharedProto.ProtocolCountingCircleSortType.Alphabetical,
                    ProtocolDomainOfInfluenceSortType = SharedProto.ProtocolDomainOfInfluenceSortType.Alphabetical,
                    CountingMachineEnabled = true,
                    EndResultFinalizeDisabled = true,
                },
            });

        var result = await RunOnDb(db => db.CantonSettings.FirstOrDefaultAsync(u => u.Id == newId));
        result.MatchSnapshot("cantonSettings");

        var affectedContests = await RunOnDb(db => db.Contests
            .AsSplitQuery()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.CantonDefaults)
            .Include(x => x.Translations)
            .Where(x => x.DomainOfInfluence.Canton == DomainOfInfluenceCanton.Tg).ToListAsync());

        foreach (var contest in affectedContests)
        {
            contest.CantonDefaults.Id = Guid.Empty;
        }

        affectedContests.MatchSnapshot("affectedContests");
    }
}
