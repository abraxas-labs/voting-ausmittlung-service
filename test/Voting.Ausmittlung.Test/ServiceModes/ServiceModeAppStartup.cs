// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Core.Utils.Snapshot;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.ServiceModes;

public class ServiceModeAppStartup : TestStartup
{
    public ServiceModeAppStartup(IConfiguration configuration, IWebHostEnvironment environment)
        : base(configuration, environment)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // required to seed mock data, but not provided in all service modes
        services.TryAddScoped<ContestSnapshotBuilder>();
        services.TryAddScoped<DomainOfInfluenceCantonDefaultsBuilder>();
        services.TryAddScoped<ContestCantonDefaultsBuilder>();
        services.TryAddScoped<ContestCountingCircleDetailsBuilder>();
        services.TryAddScoped<VoteEndResultInitializer>();
        services.TryAddScoped<SimplePoliticalBusinessBuilder<Vote>>();
        services.TryAddScoped<DomainOfInfluencePermissionBuilder>();
        services.TryAddScoped<AggregatedContestCountingCircleDetailsBuilder>();
    }
}
