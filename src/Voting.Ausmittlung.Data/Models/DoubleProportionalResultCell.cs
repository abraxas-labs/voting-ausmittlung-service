// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// A single cell of a double proportional result matrix. Represents proportional election lists.
/// </summary>
public class DoubleProportionalResultCell : BaseEntity
{
    public Guid ListId { get; set; }

    public ProportionalElectionList List { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the list reached the <see cref="DoubleProportionalResultRow.Quorum"/>.
    /// (Hint: A list can receive seats, although this quorum is not reached, if the <see cref="DoubleProportionalResultColumn.CantonalQuorumReached"/> is true).
    /// </summary>
    public bool ProportionalElectionQuorumReached { get; set; }

    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the "Wählerzahl" of the super apportionment.
    /// </summary>
    public decimal VoterNumber { get; set; }

    public int SubApportionmentNumberOfMandates => SubApportionmentNumberOfMandatesExclLotDecision + SubApportionmentNumberOfMandatesFromLotDecision;

    public int SubApportionmentNumberOfMandatesExclLotDecision { get; set; }

    public int SubApportionmentNumberOfMandatesFromLotDecision { get; set; }

    public bool SubApportionmentLotDecisionRequired { get; set; }

    public Guid RowId { get; set; }

    public DoubleProportionalResultRow Row { get; set; } = null!;

    public Guid ColumnId { get; set; }

    public DoubleProportionalResultColumn Column { get; set; } = null!;
}
