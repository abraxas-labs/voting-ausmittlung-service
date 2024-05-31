// (c) Copyright 2024 by Abraxas Informatik AG
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
                            SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum,
                    },
                    MajorityElectionAbsoluteMajorityAlgorithm = SharedProto.CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates,
                    MajorityElectionInvalidVotes = false,
                    SwissAbroadVotingRight = SharedProto.SwissAbroadVotingRight.OnEveryCountingCircle,
                    SwissAbroadVotingRightDomainOfInfluenceTypes =
                    {
                            SharedProto.DomainOfInfluenceType.Ch,
                            SharedProto.DomainOfInfluenceType.Ki,
                    },
                    ProtocolCountingCircleSortType = SharedProto.ProtocolCountingCircleSortType.SortNumber,
                    ProtocolDomainOfInfluenceSortType = SharedProto.ProtocolDomainOfInfluenceSortType.Alphabetical,
                    CountingMachineEnabled = true,
                },
            });

        var result = await RunOnDb(db => db.CantonSettings.FirstOrDefaultAsync(u => u.Id == Guid.Parse(CantonSettingsMockedData.IdStGallen)));
        result.MatchSnapshot("cantonSettings");

        var contestPastCantonDefaults = await RunOnDb(db => db.ContestCantonDefaults
            .AsSplitQuery()
            .Where(x => x.ContestId == Guid.Parse(ContestMockedData.IdVergangenerBundesurnengang))
            .ToListAsync());
        var contestInTestingPhaseCantonDefaults = await RunOnDb(db => db.ContestCantonDefaults
            .AsSplitQuery()
            .Where(x => x.ContestId == Guid.Parse(ContestMockedData.IdBundesurnengang))
            .ToListAsync());

        contestPastCantonDefaults.Any().Should().BeTrue();
        contestInTestingPhaseCantonDefaults.Any().Should().BeTrue();

        // only canton defaults in testing phase should be affected
        contestPastCantonDefaults.All(x => x.MajorityElectionAbsoluteMajorityAlgorithm == CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates).Should().BeFalse();
        contestInTestingPhaseCantonDefaults.All(x => x.MajorityElectionAbsoluteMajorityAlgorithm == CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates).Should().BeTrue();
    }
}
