// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;

namespace Voting.Ausmittlung.Middlewares;

/// <summary>
/// Band-aid middleware to compact the large object heap after specific requests.
/// </summary>
public class LargeObjectHeapCompactionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LargeObjectHeapCompactionConfig _config;
    private readonly ILogger<LargeObjectHeapCompactionMiddleware> _logger;
    private readonly Regex? _pathRegex;

    public LargeObjectHeapCompactionMiddleware(
        RequestDelegate next,
        LargeObjectHeapCompactionConfig config,
        ILogger<LargeObjectHeapCompactionMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
        _pathRegex = string.IsNullOrWhiteSpace(_config.PathRegex)
            ? null
            : new Regex(
                _config.PathRegex,
                RegexOptions.IgnoreCase | RegexOptions.Compiled,
                _config.PathRegexTimeout);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_config.Enabled || _pathRegex?.IsMatch(context.Request.Path) == false)
        {
            await _next(context);
            return;
        }

        context.Response.OnCompleted(() =>
        {
            _logger.LogInformation("Compacting memory on response completed");

            // 1) compact LOH on the next full GC
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

            // 2) full GC (phase 1: queue finalizers & compact)
            GC.Collect(
                GC.MaxGeneration,
                GCCollectionMode.Forced,
                blocking: true,
                compacting: true);

            // 3) wait for finalizers
            GC.WaitForPendingFinalizers();

            // 4) aggressive full GC (phase 2: reclaim finalized objects and decommit more)
            GC.Collect(
                GC.MaxGeneration,
                GCCollectionMode.Aggressive,
                blocking: true,
                compacting: true);

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
