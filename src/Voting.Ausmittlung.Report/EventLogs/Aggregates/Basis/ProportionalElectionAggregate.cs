// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Google.Protobuf;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

// needs to be in sync with basis.
public class ProportionalElectionAggregate : BasePoliticalBusinessAggregate
{
    public override string AggregateName => AggregateNames.ProportionalElection;

    public void Apply(ProportionalElectionCreated ev)
    {
        MapEventData(ev.ProportionalElection);
    }

    public void Apply(ProportionalElectionUpdated ev)
    {
        MapEventData(ev.ProportionalElection);
    }

    public void Apply(ProportionalElectionAfterTestingPhaseUpdated ev)
    {
        ShortDescription = MapShortDescriptionTranslations(ev.ShortDescription);
        PoliticalBusinessNumber = ev.PoliticalBusinessNumber;
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionCreated e:
                Apply(e);
                break;
            case ProportionalElectionUpdated e:
                Apply(e);
                break;
            case ProportionalElectionAfterTestingPhaseUpdated e:
                Apply(e);
                break;
        }
    }

    private void MapEventData(ProportionalElectionEventData eventData)
    {
        Id = GuidParser.Parse(eventData.Id);
        ShortDescription = MapShortDescriptionTranslations(eventData.ShortDescription);
        PoliticalBusinessNumber = eventData.PoliticalBusinessNumber;
    }
}
