// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Voting.Ausmittlung.Core.Configuration;

namespace Voting.Ausmittlung.Test.ServiceModes;

public abstract class ServiceModeAppFactory : TestApplicationFactory<ServiceModeAppStartup>
{
    private readonly ServiceMode _serviceMode;

    protected ServiceModeAppFactory(ServiceMode serviceMode)
    {
        _serviceMode = serviceMode;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration(x => x.AddInMemoryCollection(
            new Dictionary<string, string> { [nameof(AppConfig.ServiceMode)] = _serviceMode.ToString() }));
    }
}
