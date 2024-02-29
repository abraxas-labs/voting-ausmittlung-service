// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

public abstract class BasePoliticalBusinessAggregate : BaseEventSourcingAggregate, IPoliticalBusiness, IReportAggregate
{
    protected BasePoliticalBusinessAggregate()
    {
        PoliticalBusinessNumber = string.Empty;
        ShortDescription = new List<EventLogTranslation>();
    }

    public string PoliticalBusinessNumber { get; protected set; }

    public IReadOnlyCollection<EventLogTranslation> ShortDescription { get; protected set; }

    public void InitWithId(Guid id)
    {
        Id = id;

        var idStr = id.ToString();
        ShortDescription = Languages.All.Select(l => new EventLogTranslation
        {
            Language = l,
            PoliticalBusinessDescription = idStr,
        }).ToList();
    }

    protected IReadOnlyCollection<EventLogTranslation> MapShortDescriptionTranslations(IDictionary<string, string> shortDescription)
    {
        return shortDescription.Select(x => new EventLogTranslation { Language = x.Key, PoliticalBusinessDescription = x.Value }).ToList();
    }
}
