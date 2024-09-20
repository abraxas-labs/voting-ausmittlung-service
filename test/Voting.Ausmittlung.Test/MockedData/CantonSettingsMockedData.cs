// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.MockedData;

public static class CantonSettingsMockedData
{
    public const string IdStGallen = "d33e6070-4c4f-4c22-8b35-50d7a87afffe";
    public const string IdZurich = "db9fd497-321c-44b2-9441-75b610c712a1";

    public static CantonSettings StGallen
        => new CantonSettings
        {
            Id = Guid.Parse(IdStGallen),
            Canton = DomainOfInfluenceCanton.Sg,
            AuthorityName = "St.Gallen",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
            MajorityElectionAbsoluteMajorityAlgorithm = CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
            MajorityElectionInvalidVotes = false,
            SwissAbroadVotingRight = SwissAbroadVotingRight.SeparateCountingCircle,
            SwissAbroadVotingRightDomainOfInfluenceTypes = new List<DomainOfInfluenceType>
            {
                    DomainOfInfluenceType.Sk,
            },
            EnabledVotingCardChannels = new List<CantonSettingsVotingCardChannel>
            {
                    new()
                    {
                        Id = Guid.Parse("138bef4c-e0c1-45f3-a839-d31621a054af"),
                        Valid = true,
                        VotingChannel = VotingChannel.Paper,
                    },
                    new()
                    {
                        Id = Guid.Parse("1181bbb0-603b-430a-bc39-c7a936168d8a"),
                        Valid = true,
                        VotingChannel = VotingChannel.BallotBox,
                    },
                    new()
                    {
                        Id = Guid.Parse("27f8b3c6-5b2f-47f8-a366-fe25b09795e9"),
                        Valid = true,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new()
                    {
                        Id = Guid.Parse("e9d64e94-37ea-4220-9708-801a3133b06c"),
                        Valid = false,
                        VotingChannel = VotingChannel.ByMail,
                    },
            },
            ProtocolCountingCircleSortType = ProtocolCountingCircleSortType.Alphabetical,
            ProtocolDomainOfInfluenceSortType = ProtocolDomainOfInfluenceSortType.Alphabetical,
            MajorityElectionUseCandidateCheckDigit = true,
            CountingCircleResultStateDescriptions =
            {
                new CountingCircleResultStateDescription
                {
                    Id = Guid.Parse("28dc8a05-0214-4e0d-89c7-6536758f800f"),
                    State = CountingCircleResultState.SubmissionDone,
                    Description = "Erfassung beendet",
                },
            },
            PublishResultsEnabled = true,
        };

    public static CantonSettings Zurich
        => new CantonSettings
        {
            Id = Guid.Parse(IdZurich),
            Canton = DomainOfInfluenceCanton.Zh,
            AuthorityName = "Zürich",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            MajorityElectionAbsoluteMajorityAlgorithm = CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
            MajorityElectionInvalidVotes = true,
            SwissAbroadVotingRight = SwissAbroadVotingRight.OnEveryCountingCircle,
            SwissAbroadVotingRightDomainOfInfluenceTypes = new List<DomainOfInfluenceType>
            {
                    DomainOfInfluenceType.Ch,
                    DomainOfInfluenceType.Ct,
            },
            EnabledVotingCardChannels = new List<CantonSettingsVotingCardChannel>
            {
                    new()
                    {
                        Id = Guid.Parse("8b7b8a8e-4591-43c4-b449-974039ae3b49"),
                        Valid = true,
                        VotingChannel = VotingChannel.Paper,
                    },
                    new()
                    {
                        Id = Guid.Parse("fcb7696f-f670-4091-a3ee-650a4a40c4c0"),
                        Valid = true,
                        VotingChannel = VotingChannel.BallotBox,
                    },
                    new()
                    {
                        Id = Guid.Parse("eb44589b-b17a-4a5c-b3c4-e208746edb4e"),
                        Valid = true,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new()
                    {
                        Id = Guid.Parse("036f9328-85de-4532-9e25-52cfeba7ae3b"),
                        Valid = false,
                        VotingChannel = VotingChannel.ByMail,
                    },
            },
            ProtocolCountingCircleSortType = ProtocolCountingCircleSortType.SortNumber,
            ProtocolDomainOfInfluenceSortType = ProtocolDomainOfInfluenceSortType.SortNumber,
            CountingMachineEnabled = true,
            NewZhFeaturesEnabled = true,
            MajorityElectionUseCandidateCheckDigit = true,
            ProportionalElectionUseCandidateCheckDigit = true,
            CountingCircleResultStateDescriptions =
            {
                new CountingCircleResultStateDescription
                {
                    Id = Guid.Parse("df6f5549-e04d-4d0d-a0ef-6fb5c1760d15"),
                    State = CountingCircleResultState.AuditedTentatively,
                    Description = "geprüft",
                },
            },
            StatePlausibilisedDisabled = true,
            EndResultFinalizeDisabled = true,
        };

    public static IEnumerable<CantonSettings> All
    {
        get
        {
            yield return StGallen;
            yield return Zurich;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var cantonSettingsRepo = sp.GetRequiredService<CantonSettingsRepo>();
            var doiCantonDefaultsBuilder = sp.GetRequiredService<DomainOfInfluenceCantonDefaultsBuilder>();
            var contestCantonDefaultsBuilder = sp.GetRequiredService<ContestCantonDefaultsBuilder>();

            foreach (var cantonSettings in All)
            {
                await cantonSettingsRepo.Create(cantonSettings);
                await doiCantonDefaultsBuilder.RebuildForCanton(cantonSettings);
                await contestCantonDefaultsBuilder.RebuildForCanton(cantonSettings);
            }
        });
    }
}
