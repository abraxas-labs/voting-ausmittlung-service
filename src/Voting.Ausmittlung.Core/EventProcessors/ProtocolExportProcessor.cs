// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProtocolExportProcessor :
    IEventProcessor<ProtocolExportStarted>,
    IEventProcessor<ProtocolExportCompleted>,
    IEventProcessor<ProtocolExportFailed>
{
    private readonly IDbRepository<DataContext, ProtocolExport> _repo;
    private readonly ILogger<ProtocolExportProcessor> _logger;
    private readonly IMapper _mapper;
    private readonly MessageProducerBuffer _messageProducerBuffer;

    public ProtocolExportProcessor(
        IDbRepository<DataContext, ProtocolExport> repo,
        ILogger<ProtocolExportProcessor> logger,
        IMapper mapper,
        MessageProducerBuffer messageProducerBuffer,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo)
    {
        _repo = repo;
        _logger = logger;
        _mapper = mapper;
        _messageProducerBuffer = messageProducerBuffer;
    }

    public async Task Process(ProtocolExportStarted eventData)
    {
        var protocolExport = _mapper.Map<ProtocolExport>(eventData);

        if (protocolExport.CountingCircleId.HasValue)
        {
            protocolExport.CountingCircleId = AusmittlungUuidV5.BuildCountingCircleSnapshot(protocolExport.ContestId, protocolExport.CountingCircleId.Value);
        }

        // Delete previous protocol export if it was already started once
        await _repo.DeleteByKeyIfExists(protocolExport.Id);

        protocolExport.State = ProtocolExportState.Generating;
        protocolExport.Started = eventData.EventInfo.Timestamp.ToDateTime();

        await _repo.Create(protocolExport);

        PublishProtocolExportStateChangeMessage(protocolExport);
    }

    public async Task Process(ProtocolExportCompleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProtocolExportId);
        var protocolExport = await _repo.GetByKey(id);

        if (protocolExport == null)
        {
            // This may happen if a testing phase ends (protocol export is deleted), but a later callback triggers this event
            _logger.LogWarning("Protocol export {Id} not found", id);
            return;
        }

        protocolExport.State = ProtocolExportState.Completed;
        protocolExport.PrintJobId = eventData.PrintJobId;
        await _repo.Update(protocolExport);

        PublishProtocolExportStateChangeMessage(protocolExport);
    }

    public async Task Process(ProtocolExportFailed eventData)
    {
        var id = GuidParser.Parse(eventData.ProtocolExportId);
        var protocolExport = await _repo.GetByKey(id);

        if (protocolExport == null)
        {
            // This may happen if a testing phase ends (protocol export is deleted), but a later callback triggers this event
            _logger.LogWarning("Protocol export {Id} not found", id);
            return;
        }

        protocolExport.State = ProtocolExportState.Failed;
        await _repo.Update(protocolExport);

        PublishProtocolExportStateChangeMessage(protocolExport);
    }

    private void PublishProtocolExportStateChangeMessage(ProtocolExport protocolExport)
        => _messageProducerBuffer.Add(new ProtocolExportStateChanged(
            protocolExport.Id,
            protocolExport.ExportTemplateId,
            protocolExport.State,
            protocolExport.FileName,
            protocolExport.Started));
}
