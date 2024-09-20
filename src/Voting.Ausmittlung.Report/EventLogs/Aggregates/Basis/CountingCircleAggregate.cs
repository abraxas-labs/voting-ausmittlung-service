// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Google.Protobuf;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

// needs to be in sync with basis.
public class CountingCircleAggregate : BaseEventSourcingAggregate, IReportAggregate
{
    public CountingCircleAggregate()
    {
        Name = string.Empty;
        Bfs = string.Empty;
    }

    public override string AggregateName => AggregateNames.CountingCircle;

    public string Name { get; private set; }

    public string Bfs { get; private set; }

    public void InitWithId(Guid id)
    {
        Id = id;
        Name = id.ToString();
    }

    public void Apply(CountingCircleCreated ev)
    {
        MapEventData(ev.CountingCircle);
    }

    public void Apply(CountingCirclesMergerScheduled ev)
    {
        MapEventData(ev.Merger.NewCountingCircle);
    }

    public void Apply(CountingCirclesMergerScheduleUpdated ev)
    {
        MapEventData(ev.Merger.NewCountingCircle);
    }

    public void Apply(CountingCircleUpdated ev)
    {
        MapEventData(ev.CountingCircle);
    }

    public CountingCircle MapToBasisCountingCircle()
    {
        return new() { Id = Id, Name = Name, Bfs = Bfs };
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case CountingCircleCreated e:
                Apply(e);
                break;
            case CountingCirclesMergerScheduled e:
                Apply(e);
                break;
            case CountingCirclesMergerScheduleUpdated e:
                Apply(e);
                break;
            case CountingCircleUpdated e:
                Apply(e);
                break;
        }
    }

    private void MapEventData(CountingCircleEventData eventData)
    {
        Id = GuidParser.Parse(eventData.Id);
        Name = eventData.Name;
        Bfs = eventData.Bfs;
    }
}
