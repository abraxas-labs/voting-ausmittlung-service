// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// A column of a double proportional result matrix. Represents a proportional election list or proportional election union list.
/// </summary>
public class DoubleProportionalResultColumn : BaseEntity
{
    public Guid? UnionListId { get; set; }

    public ProportionalElectionUnionList? UnionList { get; set; } = null!;

    public Guid? ListId { get; set; }

    public ProportionalElectionList? List { get; set; }

    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the union list reached the <see cref="DoubleProportionalResult.CantonalQuorum"/>.
    /// </summary>
    public bool CantonalQuorumReached { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether whether the the union list reached the <see cref="DoubleProportionalResult.CantonalQuorum"/> if required
    /// or any corresponding <see cref="DoubleProportionalResultCell.ProportionalElectionQuorumReached"/>.
    /// </summary>
    public bool AnyRequiredQuorumReached { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the "Wählerzahl" (Sum of all corresponding lists).
    /// </summary>
    public decimal VoterNumber { get; set; }

    public decimal SuperApportionmentQuotient { get; set; }

    /// <summary>
    /// Gets or a value indicating how many number of mandates the union list is expected to receive according the "Oberzuteilung".
    /// </summary>
    public int SuperApportionmentNumberOfMandates => SuperApportionmentNumberOfMandatesExclLotDecision + SuperApportionmentNumberOfMandatesFromLotDecision;

    public int SuperApportionmentNumberOfMandatesExclLotDecision { get; set; }

    public int SuperApportionmentNumberOfMandatesFromLotDecision { get; set; }

    public bool SuperApportionmentLotDecisionRequired { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    public decimal Divisor { get; set; }

    public int SubApportionmentInitialNegativeTies { get; set; }

    public bool HasSubApportionmentOpenLotDecision => SubApportionmentNumberOfMandates > 0 &&
        SuperApportionmentNumberOfMandates != SubApportionmentNumberOfMandates;

    public Guid ResultId { get; set; }

    public DoubleProportionalResult Result { get; set; } = null!;

    public ICollection<DoubleProportionalResultCell> Cells { get; set; } = new List<DoubleProportionalResultCell>();
}
