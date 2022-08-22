// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using Voting.Lib.VotingExports.Models;

namespace Microsoft.Extensions.DependencyInjection;

public interface IReportingServiceCollection
{
    public IReportingServiceCollection AddRendererService<TRenderer>(params TemplateModel[] templates)
        where TRenderer : class, IRendererService;
}
