// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.Extensions.DependencyInjection.Extensions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using Voting.Lib.VotingExports.Models;

namespace Microsoft.Extensions.DependencyInjection;

internal class ReportingServiceCollection : IReportingServiceCollection
{
    private readonly ResultRenderServiceRegistry _resultRenderServiceRegistry = new();

    public ReportingServiceCollection(IServiceCollection services)
    {
        Services = services;
        Services.TryAddScoped<ResultRenderServiceAdapter>();
        Services.TryAddSingleton(_resultRenderServiceRegistry);
    }

    public IServiceCollection Services { get; }

    public IReportingServiceCollection AddRendererService<TRenderer>(params TemplateModel[] templates)
        where TRenderer : class, IRendererService
    {
        _resultRenderServiceRegistry.Add<TRenderer>(templates);
        Services.AddScoped<TRenderer>();
        return this;
    }
}
