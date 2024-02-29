// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;
using Voting.Ausmittlung.Core.Utils.Snapshot;
using Voting.Lib.Eventing;
using Voting.Lib.Eventing.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

internal static class EventProcessorServiceCollection
{
    internal static IServiceCollection AddEventProcessorServices(this IServiceCollection services, AppConfig config)
    {
        if (!config.EventProcessorModeEnabled)
        {
            return services;
        }

        return services
            .AddScoped<ContestCountingCircleDetailsBuilder>()
            .AddScoped<ContestResultInitializer>()
            .AddScoped<AggregatedContestCountingCircleDetailsBuilder>()
            .AddScoped<CountingCircleResultsInitializer>()
            .AddScoped<DomainOfInfluencePermissionBuilder>()
            .AddScoped<DomainOfInfluenceCountingCircleInheritanceBuilder>()
            .AddScoped<ProportionalElectionUnionListBuilder>()
            .AddScoped<DomainOfInfluenceCantonDefaultsBuilder>()
            .AddScoped<ProportionalElectionEndResultLotDecisionBuilder>()
            .AddScoped<ProportionalElectionEndResultBuilder>()
            .AddScoped<ProportionalElectionResultBallotBuilder>()
            .AddScoped<ProportionalElectionCandidateEndResultBuilder>()
            .AddScoped<MajorityElectionResultBallotBuilder>()
            .AddScoped<MajorityElectionEndResultInitializer>()
            .AddScoped<MajorityElectionCandidateEndResultBuilder>()
            .AddScoped<MajorityElectionEndResultBuilder>()
            .AddScoped<VoteResultBallotBuilder>()
            .AddScoped<VoteEndResultInitializer>()
            .AddScoped<VoteEndResultBuilder>()
            .AddScoped<ProportionalElectionEndResultInitializer>()
            .AddScoped<ContestSnapshotBuilder>()
            .AddScoped(typeof(SimplePoliticalBusinessBuilder<>))
            .AddScoped(typeof(PoliticalBusinessToNewContestMover<,>))
            .AddScoped<ResultExportConfigurationBuilder>()
            .AddSingleton<MajorityElectionStrategyFactory>()
            .AddSingleton<IMajorityElectionMandateAlgorithmStrategy, MajorityElectionAbsoluteMajorityStrategy>()
            .AddSingleton<IMajorityElectionMandateAlgorithmStrategy, MajorityElectionAbsoluteMajorityCandidateVotesDividedByTheDoubleOfNumberOfMandatesStrategy>()
            .AddSingleton<IMajorityElectionMandateAlgorithmStrategy, MajorityElectionRelativeMajorityStrategy>();
    }

    internal static IEventingServiceCollection AddEventProcessors(this IEventingServiceCollection services, AppConfig config)
    {
        return config.EventProcessorModeEnabled
            ? services.AddSubscription<EventProcessorScope>(WellKnownStreams.CategoryVoting)
            : services;
    }
}
