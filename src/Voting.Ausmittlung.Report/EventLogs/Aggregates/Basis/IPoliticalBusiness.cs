// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

internal interface IPoliticalBusiness
{
    string PoliticalBusinessNumber { get; }

    IReadOnlyCollection<EventLogTranslation> ShortDescription { get; }
}
