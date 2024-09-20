// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Ausmittlung.TemporaryData.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Core.Services.Export;

public class ExportRateLimitService
{
    private readonly IClock _clock;
    private readonly ExportLogEntryRepo _reportExportEntryRepo;
    private readonly Dictionary<ExportFileFormat, ExportTypeRateLimitConfig> _rateLimitConfigByFormat;
    private readonly IAuth _auth;
    private readonly ExportRateLimitConfig _rateLimitConfig;
    private readonly TemporaryDataContext _dbContext;

    public ExportRateLimitService(
        IClock clock,
        ExportLogEntryRepo reportExportEntryRepo,
        IAuth auth,
        PublisherConfig config,
        TemporaryDataContext dbContext)
    {
        _clock = clock;
        _reportExportEntryRepo = reportExportEntryRepo;
        _auth = auth;
        _rateLimitConfig = config.ExportRateLimit;
        _dbContext = dbContext;

        _rateLimitConfigByFormat = new Dictionary<ExportFileFormat, ExportTypeRateLimitConfig>
        {
            { ExportFileFormat.Pdf, _rateLimitConfig.ProtocolRateLimit },
            { ExportFileFormat.Csv, _rateLimitConfig.DataRateLimit },
            { ExportFileFormat.Xml, _rateLimitConfig.DataRateLimit },
        };
    }

    internal Task CheckAndLog(IReadOnlyCollection<ResultExportTemplate> exportTemplates)
        => CheckAndLog(exportTemplates.Select(t => (t.Template.Key, t.Template.Format)).ToList());

    internal async Task CheckAndLog(IReadOnlyCollection<(string Key, ExportFileFormat Format)> exports)
    {
        if (_rateLimitConfig.Disabled)
        {
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        if (exports.Count > 1)
        {
            throw new NotSupportedException($"{nameof(CheckAndLog)} is not supported for multiple templates");
        }

        var export = exports.Single();

        if (!_rateLimitConfigByFormat.TryGetValue(export.Format, out var rateLimitConfig))
        {
            throw new InvalidOperationException($"Cannot find rate limit config for export format {export.Format}");
        }

        var to = _clock.UtcNow;
        var from = to.Subtract(rateLimitConfig.TimeSpan.Duration());
        var hasReachedLimit = await _reportExportEntryRepo.HasReachedLimitAndLock(export.Key, _auth.Tenant.Id, from, to, rateLimitConfig.MaxExportsPerTimeSpan);

        if (hasReachedLimit)
        {
            throw new ForbiddenException($"Rate limit reached for export {export.Key}. Try later again");
        }

        await CreateLogEntries(exports.Select(e => e.Key).ToList());
        await transaction.CommitAsync();
    }

    private async Task CreateLogEntries(IReadOnlyCollection<string> exportKeys)
    {
        var now = _clock.UtcNow;
        var tenantId = _auth.Tenant.Id;

        await _reportExportEntryRepo.CreateRange(exportKeys.Select(exportKey => new ExportLogEntry
        {
            ExportKey = exportKey,
            TenantId = tenantId,
            Timestamp = now,
        }));
    }
}
