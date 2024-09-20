// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Google.Protobuf;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

// needs to be in sync with basis.
public class VoteAggregate : BasePoliticalBusinessAggregate
{
    public override string AggregateName => AggregateNames.Vote;

    public void Apply(VoteCreated ev)
    {
        MapEventData(ev.Vote);
    }

    public void Apply(VoteUpdated ev)
    {
        MapEventData(ev.Vote);
    }

    public void Apply(VoteAfterTestingPhaseUpdated ev)
    {
        ShortDescription = MapShortDescriptionTranslations(ev.ShortDescription);
        PoliticalBusinessNumber = ev.PoliticalBusinessNumber;
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case VoteCreated e:
                Apply(e);
                break;
            case VoteUpdated e:
                Apply(e);
                break;
            case VoteAfterTestingPhaseUpdated e:
                Apply(e);
                break;
        }
    }

    private void MapEventData(VoteEventData eventData)
    {
        Id = GuidParser.Parse(eventData.Id);
        ShortDescription = MapShortDescriptionTranslations(eventData.ShortDescription);
        PoliticalBusinessNumber = eventData.PoliticalBusinessNumber;
    }
}
