// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Google.Protobuf;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

// needs to be in sync with basis.
public class MajorityElectionAggregate : BasePoliticalBusinessAggregate
{
    public MajorityElectionAggregate()
    {
        SecondaryMajorityElections = new();
    }

    public override string AggregateName => AggregateNames.MajorityElection;

    public Dictionary<Guid, SecondaryMajorityElection> SecondaryMajorityElections { get; private set; }

    public void Apply(MajorityElectionCreated ev)
    {
        MapEventData(ev.MajorityElection);
    }

    public void Apply(MajorityElectionUpdated ev)
    {
        MapEventData(ev.MajorityElection);
    }

    public void Apply(SecondaryMajorityElectionCreated ev)
    {
        MapEventData(ev.SecondaryMajorityElection);
    }

    public void Apply(SecondaryMajorityElectionUpdated ev)
    {
        MapEventData(ev.SecondaryMajorityElection);
    }

    public void Apply(MajorityElectionAfterTestingPhaseUpdated ev)
    {
        ShortDescription = MapShortDescriptionTranslations(ev.ShortDescription);
        PoliticalBusinessNumber = ev.PoliticalBusinessNumber;
    }

    public void Apply(SecondaryMajorityElectionAfterTestingPhaseUpdated ev)
    {
        var smeId = Guid.Parse(ev.Id);
        var sme = SecondaryMajorityElections[smeId];
        sme.ShortDescription = MapShortDescriptionTranslations(ev.ShortDescription);
    }

    public void Apply(SecondaryMajorityElectionDeleted ev)
    {
        var smeId = Guid.Parse(ev.SecondaryMajorityElectionId);
        SecondaryMajorityElections.Remove(smeId);
    }

    public SecondaryMajorityElection GetSecondaryMajorityElection(Guid smeId)
    {
        return SecondaryMajorityElections.GetValueOrDefault(smeId) ??
            throw new ArgumentException($"Secondary majority election with id {smeId} not found");
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case MajorityElectionCreated e:
                Apply(e);
                break;
            case MajorityElectionUpdated e:
                Apply(e);
                break;
            case MajorityElectionAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionCreated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionAfterTestingPhaseUpdated e:
                Apply(e);
                break;
            case SecondaryMajorityElectionDeleted e:
                Apply(e);
                break;
        }
    }

    private void MapEventData(MajorityElectionEventData eventData)
    {
        Id = GuidParser.Parse(eventData.Id);
        ShortDescription = MapShortDescriptionTranslations(eventData.ShortDescription);
        PoliticalBusinessNumber = eventData.PoliticalBusinessNumber;
    }

    private void MapEventData(SecondaryMajorityElectionEventData eventData)
    {
        var smeId = Guid.Parse(eventData.Id);
        var sme = SecondaryMajorityElections.GetValueOrDefault(smeId);

        if (sme == null)
        {
            sme = new();
            SecondaryMajorityElections.Add(smeId, sme);
        }

        sme.Id = smeId;
        sme.ShortDescription = MapShortDescriptionTranslations(eventData.ShortDescription);
        sme.PoliticalBusinessNumber = eventData.PoliticalBusinessNumber;
    }
}
