// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ResultExportConfigurationAggregate : BaseEventSourcingAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;

    public ResultExportConfigurationAggregate(EventInfoProvider eventInfoProvider)
    {
        _eventInfoProvider = eventInfoProvider;
    }

    public override string AggregateName => "voting-resultExportConfiguration";

    public void UpdateFrom(ResultExportConfiguration config, Guid contestId, Guid configId)
    {
        Id = AusmittlungUuidV5.BuildResultExportConfiguration(contestId, configId);

        if (config.IntervalMinutes is < 1)
        {
            throw new ValidationException($"{config.IntervalMinutes} has to be at least 1");
        }

        var eventData = new ResultExportConfigurationEventData
        {
            ContestId = contestId.ToString(),
            IntervalMinutes = config.IntervalMinutes,
            ExportConfigurationId = configId.ToString(),
            PoliticalBusinessIds = { config.PoliticalBusinessIds.Select(x => x.ToString()) },
        };

        foreach (var metadata in config.PoliticalBusinessMetadata)
        {
            eventData.PoliticalBusinessMetadata.Add(metadata.Key.ToString(), new PoliticalBusinessExportMetadataEventData
            {
                Token = metadata.Value.Token,
            });
        }

        RaiseEvent(new ResultExportConfigurationUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ExportConfiguration = eventData,
        });
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ResultExportConfigurationUpdated ev:
                Id = AusmittlungUuidV5.BuildResultExportConfiguration(
                    GuidParser.Parse(ev.ExportConfiguration.ContestId),
                    GuidParser.Parse(ev.ExportConfiguration.ExportConfigurationId));
                break;
        }
    }
}
