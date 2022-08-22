// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.CantonSettingsTests;

public class CantonSettingsUpdateTest : BaseDataProcessorTest
{
    public CantonSettingsUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestUpdated()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new CantonSettingsUpdated
            {
                CantonSettings = new CantonSettingsEventData
                {
                    Id = CantonSettingsMockedData.IdStGallen,
                    Canton = SharedProto.DomainOfInfluenceCanton.Sg,
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    AuthorityName = "St.Gallen Update",
                    ProportionalElectionMandateAlgorithms =
                    {
                            SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                            SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum,
                    },
                    MajorityElectionAbsoluteMajorityAlgorithm = SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
                    MajorityElectionInvalidVotes = false,
                    SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.OnEveryCountingCircle,
                    SwissAbroadVotingRightDomainOfInfluenceTypes =
                    {
                            SharedProto.DomainOfInfluenceType.Ch,
                            SharedProto.DomainOfInfluenceType.Ki,
                    },
                },
            });

        var result = await RunOnDb(db => db.CantonSettings.FirstOrDefaultAsync(u => u.Id == Guid.Parse(CantonSettingsMockedData.IdStGallen)));
        result.MatchSnapshot("cantonSettings");

        var contestPastDois = await RunOnDb(db => db.DomainOfInfluences
            .Where(doi => doi.Canton == DomainOfInfluenceCanton.Sg && doi.SnapshotContestId == Guid.Parse(ContestMockedData.IdVergangenerBundesurnengang))
            .ToListAsync());
        var contestInTestingPhaseDois = await RunOnDb(db => db.DomainOfInfluences
            .Where(doi => doi.Canton == DomainOfInfluenceCanton.Sg && doi.SnapshotContestId == Guid.Parse(ContestMockedData.IdBundesurnengang))
            .ToListAsync());
        var baseDois = await RunOnDb(db => db.DomainOfInfluences
            .Where(doi => doi.Canton == DomainOfInfluenceCanton.Sg && doi.SnapshotContestId == null)
            .ToListAsync());

        contestPastDois.Any().Should().BeTrue();
        contestInTestingPhaseDois.Any().Should().BeTrue();
        baseDois.Any().Should().BeTrue();

        // only base dois and dois in testing phase should be affected
        contestPastDois.All(doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms.Count == 2).Should().BeFalse();
        contestInTestingPhaseDois.All(doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms.Count == 2).Should().BeTrue();
        baseDois.All(doi => doi.CantonDefaults.ProportionalElectionMandateAlgorithms.Count == 2).Should().BeTrue();
    }
}
