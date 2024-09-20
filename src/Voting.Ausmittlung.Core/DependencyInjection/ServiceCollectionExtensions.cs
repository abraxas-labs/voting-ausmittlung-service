// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Core.Utils;
using ProtoBasis = Abraxas.Voting.Basis.Events.V1.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, AppConfig config)
    {
        return services
            .AddEventing(config)
            .AddPublisherServices(config)
            .AddEventProcessorServices(config)
            .AddCommon()
            .AddSystemClock()
            .AddCryptography(config);
    }

    private static IServiceCollection AddEventing(this IServiceCollection services, AppConfig config)
    {
        return services.AddVotingLibEventing(config.EventStore, typeof(EventInfo).Assembly, typeof(ProtoBasis.EventInfo).Assembly)
            .AddEventProcessors(config)
            .AddPublisher(config)
            .Services;
    }

    private static IServiceCollection AddCommon(this IServiceCollection services)
    {
        // common services which are used to compute transient reader service results
        // and to compute results for the read model.
        return services
            .AddScoped<VoteResultBuilder>()
            .AddScoped<MajorityElectionResultBuilder>()
            .AddScoped<MajorityElectionBallotGroupResultBuilder>()
            .AddScoped<MajorityElectionCandidateResultBuilder>()
            .AddScoped<ProportionalElectionResultBuilder>()
            .AddScoped<ProportionalElectionCandidateResultBuilder>();
    }

    private static IServiceCollection AddCryptography(this IServiceCollection services, AppConfig config)
    {
        if (!config.EnablePkcs11Mock)
        {
            services.AddVotingLibPkcs11(config.Pkcs11);
        }
        else
        {
            services.AddVotingLibPkcs11Mock();
        }

        return services
            .AddVotingLibCryptography()
            .AddEventSignature()
            .AddSingleton(config.EventSignature)
            .AddSingleton(config.Machine)
            .AddSingleton<EventSignatureService>();
    }
}
