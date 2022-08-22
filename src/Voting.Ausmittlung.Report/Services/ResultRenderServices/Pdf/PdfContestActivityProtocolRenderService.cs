// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Protobuf;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfContestActivityProtocolRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IProtobufTypeRegistry _protoRegistry;
    private readonly IMapper _mapper;
    private readonly ILogger<PdfContestActivityProtocolRenderService> _logger;
    private readonly EventLogsBuilder _eventLogsBuilder;

    public PdfContestActivityProtocolRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        IMapper mapper,
        IProtobufTypeRegistry protoRegistry,
        ILogger<PdfContestActivityProtocolRenderService> logger,
        EventLogsBuilder eventLogsBuilder)
    {
        _templateService = templateService;
        _contestRepo = contestRepo;
        _mapper = mapper;
        _protoRegistry = protoRegistry;
        _logger = logger;
        _eventLogsBuilder = eventLogsBuilder;
    }

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(c => c.Translations)
            .Include(c => c.SimplePoliticalBusinesses)
            .FirstAsync(c => c.Id == ctx.ContestId, ct);

        var pdfActivityProtocol = new PdfActivityProtocol
        {
            TemplateKey = ctx.Template.Key,
            Contest = _mapper.Map<PdfContest>(contest),
        };

        await foreach (var eventLog in _eventLogsBuilder.Build(contest, ctx.PoliticalBusinessIds).WithCancellation(ct))
        {
            var pdfEvent = new PdfEvent
            {
                Date = eventLog.Timestamp,
                EventName = eventLog.EventFullName,
                EventData = GetEventAttributes(eventLog).ToList(),
                Tenant = _mapper.Map<PdfEventTenant>(eventLog.EventTenant),
                User = _mapper.Map<PdfEventUser>(eventLog.EventUser),
                CountingCircle = _mapper.Map<PdfEventCountingCircle>(eventLog.CountingCircle),
                PoliticalBusiness = _mapper.Map<PdfEventPoliticalBusiness>(eventLog),
                BundleBallotNumber = eventLog.BundleBallotNumber,
                BundleNumber = eventLog.BundleNumber,
                EventSignatureVerification = (PdfEventSignatureVerification)eventLog.EventSignatureVerification,
            };
            pdfActivityProtocol.Events.Add(pdfEvent);
        }

        return await _templateService.RenderToPdf(ctx, pdfActivityProtocol);
    }

    private IEnumerable<PdfEventAttribute> GetEventAttributes(EventLog eventLog)
    {
        var descriptor = _protoRegistry.Find(eventLog.EventFullName);
        if (descriptor == null)
        {
            _logger.LogError("could not find proto information for event {EventFullName}, skipping details for audit log", eventLog.EventFullName);
            return Enumerable.Empty<PdfEventAttribute>();
        }

        var message = descriptor.Parser.ParseFrom(eventLog.EventContent);
        return GetEventAttributes(message);
    }

    private IEnumerable<PdfEventAttribute> GetEventAttributes(IMessage? message)
    {
        if (message == null)
        {
            yield break;
        }

        foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
        {
            if (field.FieldType is FieldType.Bytes or FieldType.Group)
            {
                continue;
            }

            var value = field.Accessor.GetValue(message);
            foreach (var attribute in BuildAttributes(field.Name, value))
            {
                yield return attribute;
            }
        }
    }

    private IEnumerable<PdfEventAttribute> BuildAttributes(string name, object? value)
    {
        switch (value)
        {
            case MapField<string, string> mapValue:
                return new[] { BuildAttributes(name, mapValue) };
            case IList repeatedValue:
                return BuildAttributes(name, repeatedValue);
            case IMessage childMsg:
                var children = GetEventAttributes(childMsg).ToList();
                if (children.Count == 0)
                {
                    return Enumerable.Empty<PdfEventAttribute>();
                }

                return new[]
                {
                        new PdfEventAttribute
                        {
                            Name = name,
                            Children = children,
                        },
                };
            case { } o:
                return new[]
                {
                        new PdfEventAttribute
                        {
                            Name = name,
                            Value = o.ToString(),
                        },
                };
        }

        return Enumerable.Empty<PdfEventAttribute>();
    }

    private PdfEventAttribute BuildAttributes(string name, MapField<string, string> dict)
    {
        return new PdfEventAttribute { Name = name, Children = dict.Where(x => x.Key != null && x.Value != null).SelectMany(x => BuildAttributes(x.Key, (object?)x.Value)).ToList() };
    }

    private IEnumerable<PdfEventAttribute> BuildAttributes(string name, IEnumerable list)
    {
        return list
            .Cast<object>()
            .SelectMany(x => BuildAttributes(name, x));
    }
}
