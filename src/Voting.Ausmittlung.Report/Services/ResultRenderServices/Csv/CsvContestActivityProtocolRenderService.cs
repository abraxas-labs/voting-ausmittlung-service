// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.Converter;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv;

public class CsvContestActivityProtocolRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly EventLogsBuilder _eventLogsBuilder;
    private readonly EventLogBuilderContextBuilder _eventLogBuilderContextBuilder;

    public CsvContestActivityProtocolRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        EventLogsBuilder eventLogsBuilder,
        EventLogBuilderContextBuilder eventLogBuilderContextBuilder)
    {
        _templateService = templateService;
        _contestRepo = contestRepo;
        _eventLogsBuilder = eventLogsBuilder;
        _eventLogBuilderContextBuilder = eventLogBuilderContextBuilder;
    }

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(c => c.Translations)
            .Include(c => c.SimplePoliticalBusinesses)
            .AsSplitQuery()
            .FirstAsync(c => c.Id == ctx.ContestId, ct);

        var events = BuildEventLog(contest, ct);
        return _templateService.RenderToCsv(
            ctx,
            events);
    }

    private async IAsyncEnumerable<CsvEvent> BuildEventLog(Contest contest, [EnumeratorCancellation] CancellationToken ct)
    {
        var pbIds = contest.SimplePoliticalBusinesses.Select(x => x.Id);
        var eventLogBuilderContext = await _eventLogBuilderContextBuilder.BuildContext(contest.Id, pbIds);

        await foreach (var eventLog in _eventLogsBuilder.BuildBusinessEventLogs(contest, eventLogBuilderContext).WithCancellation(ct))
        {
            yield return ToCsvEvent(eventLog);
        }

        // build public key signature event logs after the business event logs are built, because it needs to process
        // all business events first, to have informations such as the signed event count.
        foreach (var eventLog in _eventLogsBuilder.BuildPublicKeySignatureEventLogs(eventLogBuilderContext))
        {
            yield return ToCsvEvent(eventLog);
        }
    }

    private CsvEvent ToCsvEvent(EventLog eventLog)
    {
        var politicalBusinessDescription = eventLog.Translations.FirstOrDefault()?.PoliticalBusinessDescription;
        var politicalBusinessEntry = $"{eventLog.PoliticalBusinessNumber} {politicalBusinessDescription}".Trim();

        return new CsvEvent
        {
            Date = eventLog.Timestamp,
            EventName = eventLog.EventFullName,
            TenantName = eventLog.EventTenant?.TenantName ?? string.Empty,
            UserName = eventLog.EventUser?.Username ?? string.Empty,
            FullName = eventLog.EventUser == null
                ? string.Empty
                : $"{eventLog.EventUser.Firstname} {eventLog.EventUser.Lastname}",
            CountingCircle = eventLog.CountingCircle == null
                ? string.Empty
                : $"{eventLog.CountingCircle.Bfs} {eventLog.CountingCircle.Name}",
            PoliticalBusiness = politicalBusinessEntry,
            EventSignatureVerification = eventLog.EventSignatureVerification,
            BundleNumber = eventLog.BundleNumber,
            BundleBallotNumber = eventLog.BundleBallotNumber,
            EventData = JsonFormatter.Default.Format(eventLog.EventContent),
        };
    }

    private class CsvEvent
    {
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime Date { get; init; }

        public string EventName { get; init; } = string.Empty;

        public string TenantName { get; init; } = string.Empty;

        public string UserName { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;

        public string CountingCircle { get; init; } = string.Empty;

        public string PoliticalBusiness { get; init; } = string.Empty;

        [TypeConverter(typeof(EventSignatureVerificationConverter))]
        public EventLogEventSignatureVerification? EventSignatureVerification { get; init; }

        public int? BundleNumber { get; init; }

        public int? BundleBallotNumber { get; init; }

        public string EventData { get; init; } = string.Empty;
    }
}
