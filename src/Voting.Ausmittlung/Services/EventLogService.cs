// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Lib.Common;
using Voting.Lib.Iam.Authorization;
using Permissions = Voting.Ausmittlung.Core.Authorization.Permissions;
using ServiceBase = Abraxas.Voting.Ausmittlung.Services.V1.EventLogService.EventLogServiceBase;

namespace Voting.Ausmittlung.Services;

public class EventLogService : ServiceBase
{
    private readonly EventLogReader _eventLogReader;
    private readonly IMapper _mapper;

    public EventLogService(EventLogReader eventLogReader, IMapper mapper)
    {
        _eventLogReader = eventLogReader;
        _mapper = mapper;
    }

    [AuthorizePermission(Permissions.EventLog.Watch)]
    public override Task Watch(WatchEventsRequest request, IServerStreamWriter<Event> responseStream, ServerCallContext context)
    {
        var contestId = GuidParser.Parse(request.ContestId);
        var basisCountingCircleId = GuidParser.ParseNullable(request.CountingCircleId);
        var filters = request.Filters.Select(f => new EventLogReader.EventFilter(
                f.Id,
                f.Types_.ToHashSet(),
                GuidParser.ParseNullable(f.PoliticalBusinessId),
                GuidParser.ParseNullable(f.PoliticalBusinessResultId),
                GuidParser.ParseNullable(f.PoliticalBusinessUnionId)))
            .ToList();

        Task Listener(string filterId, EventProcessedMessage e)
        {
            return responseStream.WriteAsync(new Event
            {
                Type = e.EventType,
                FilterId = filterId,
                Timestamp = e.Timestamp.ToTimestamp(),
                AggregateId = e.AggregateId.ToString(),
                EntityId = e.EntityId?.ToString() ?? string.Empty,
                ContestId = e.ContestId?.ToString() ?? string.Empty,
                PoliticalBusinessId = e.PoliticalBusinessId?.ToString() ?? string.Empty,
                PoliticalBusinessUnionId = e.PoliticalBusinessUnionId?.ToString() ?? string.Empty,
                PoliticalBusinessBundleId = e.PoliticalBusinessResultBundleId?.ToString() ?? string.Empty,
                Data = _mapper.Map<EventDetails>(e.Details),
            });
        }

        return _eventLogReader.Watch(contestId, basisCountingCircleId, filters, Listener, context.CancellationToken);
    }
}
