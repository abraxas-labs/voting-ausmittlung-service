// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfContestActivityProtocolRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IMapper _mapper;
    private readonly EventLogsBuilder _eventLogsBuilder;
    private readonly EventLogBuilderContextBuilder _eventLogBuilderContextBuilder;
    private readonly IClock _clock;

    public PdfContestActivityProtocolRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        IMapper mapper,
        EventLogsBuilder eventLogsBuilder,
        EventLogBuilderContextBuilder eventLogBuilderContextBuilder,
        IClock clock)
    {
        _templateService = templateService;
        _contestRepo = contestRepo;
        _mapper = mapper;
        _eventLogsBuilder = eventLogsBuilder;
        _eventLogBuilderContextBuilder = eventLogBuilderContextBuilder;
        _clock = clock;
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

        var pbIds = contest.SimplePoliticalBusinesses.Select(x => x.Id);

        var pdfActivityProtocol = new PdfActivityProtocol
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
            Contest = _mapper.Map<PdfContest>(contest),
        };

        var eventLogBuilderContext = await _eventLogBuilderContextBuilder.BuildContext(contest.Id, pbIds);

        await foreach (var eventLog in _eventLogsBuilder.BuildBusinessEventLogs(contest, eventLogBuilderContext).WithCancellation(ct))
        {
            if (eventLog.EventSignatureVerification == EventLogEventSignatureVerification.VerificationSuccess)
            {
                // Skip all events with a verified signature, which should be most of the events
                // Otherwise, the PDF would get very big and likely couldn't be generated
                continue;
            }

            var pdfEvent = new PdfEvent
            {
                Date = eventLog.Timestamp,
                EventName = eventLog.EventFullName,
                EventData = GetEventAttributes(eventLog.EventContent).ToList(),
                Tenant = _mapper.Map<PdfEventTenant>(eventLog.EventTenant),
                User = _mapper.Map<PdfEventUser>(eventLog.EventUser),
                CountingCircle = _mapper.Map<PdfEventCountingCircle>(eventLog.CountingCircle),
                PoliticalBusiness = _mapper.Map<PdfEventPoliticalBusiness>(eventLog),
                BundleBallotNumber = eventLog.BundleBallotNumber,
                BundleNumber = eventLog.BundleNumber,
                EventSignatureVerification = (PdfEventSignatureVerification?)eventLog.EventSignatureVerification,
            };
            pdfActivityProtocol.Events.Add(pdfEvent);
        }

        // build public key signature event logs after the business event logs are built, because it needs to process
        // all business events first, to have informations such as the signed event count.
        foreach (var eventLog in _eventLogsBuilder.BuildPublicKeySignatureEventLogs(eventLogBuilderContext))
        {
            var pdfPublicKeySignatureEvent = new PdfEvent
            {
                Date = eventLog.Timestamp,
                EventName = eventLog.EventFullName,
                EventData = GetEventAttributes(eventLog.EventContent).ToList(),
                Tenant = _mapper.Map<PdfEventTenant>(eventLog.EventTenant),
                User = _mapper.Map<PdfEventUser>(eventLog.EventUser),
                PublicKeyData = _mapper.Map<PdfEventPublicKeyData>(eventLog.PublicKeyData),
            };
            pdfActivityProtocol.PublicKeySignatureEvents.Add(pdfPublicKeySignatureEvent);
        }

        return await _templateService.RenderToPdf(ctx, pdfActivityProtocol);
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
