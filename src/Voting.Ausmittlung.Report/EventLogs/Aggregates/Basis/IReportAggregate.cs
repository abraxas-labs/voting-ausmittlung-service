// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

/// <summary>
/// Represents an aggregate used in the reports.
/// </summary>
public interface IReportAggregate
{
    /// <summary>
    /// If an aggregate cannot be found in the eventstore we create it on the fly.
    /// This method initializes such aggregates with the id.
    /// This id should be stored in user-visible fields such as the name,
    /// that a correlation on the activity protocol can be done by a user.
    /// </summary>
    /// <param name="id">The id of the aggregate.</param>
    void InitWithId(Guid id);
}
