// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test;

public class TestApplicationFactory : TestApplicationFactory<TestStartup>
{
}

public class TestApplicationFactory<TStartup> : BaseTestApplicationFactory<TStartup>, IAsyncLifetime
    where TStartup : Startup
{
    Task IAsyncLifetime.InitializeAsync()
    {
        return Services.GetRequiredService<InMemoryTestHarness>().Start();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return Services.GetRequiredService<InMemoryTestHarness>().Stop();
    }

    public override HttpClient CreateHttpClient(
        bool authorize,
        string? tenant,
        string? userId,
        string[]? roles,
        IEnumerable<(string, string)>? additionalHeaders = null)
    {
        var httpClient = base.CreateHttpClient(authorize, tenant, userId, roles, additionalHeaders);
        httpClient.DefaultRequestHeaders.Add("x-language", "de");
        return httpClient;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder
            .UseEnvironment("Test")
            .UseSolutionRelativeContentRoot("src/Voting.Ausmittlung");
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        return base.CreateHostBuilder()
            .UseSerilog((context, _, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
            .ConfigureAppConfiguration((_, config) =>
            {
                // we deploy our config with the docker image, no need to watch for changes
                foreach (var source in config.Sources.OfType<JsonConfigurationSource>())
                {
                    source.ReloadOnChange = false;
                }
            });
    }
}
