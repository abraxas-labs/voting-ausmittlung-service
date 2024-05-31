// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// The double proportional result of a proportional election (<see cref="ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum"/>)
/// or proportional election union (<see cref="ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum"/> or <see cref="ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum"/>).
/// </summary>
public class DoubleProportionalResult : BaseEntity
{
    public Guid? ProportionalElectionUnionId { get; set; }

    public ProportionalElectionUnion? ProportionalElectionUnion { get; set; }

    public Guid? ProportionalElectionId { get; set; }

    public ProportionalElection? ProportionalElection { get; set; }

    public ProportionalElectionMandateAlgorithm MandateAlgorithm { get; set; }

    public int NumberOfMandates { get; set; }

    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the cantonal quorum (only relevant for <see cref="ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum"/>).
    /// </summary>
    public int CantonalQuorum { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the "Wählerzahl" (Sum of all corresponding cells/lists).
    /// </summary>
    public decimal VoterNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the "Wahlschlüssel" of the super apportionment.
    /// </summary>
    public decimal ElectionKey { get; set; }

    public bool AllNumberOfMandatesDistributed => SuperApportionmentState == DoubleProportionalResultApportionmentState.Completed
        && (ProportionalElectionUnionId == null || SubApportionmentState == DoubleProportionalResultApportionmentState.Completed);

    public int SuperApportionmentNumberOfMandates { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    public bool HasSuperApportionmentRequiredLotDecision { get; set; }

    public bool HasSubApportionmentRequiredLotDecision { get; set; }

    public DoubleProportionalResultApportionmentState SuperApportionmentState { get; set; } = DoubleProportionalResultApportionmentState.Initial;

    public DoubleProportionalResultApportionmentState SubApportionmentState { get; set; } = DoubleProportionalResultApportionmentState.Initial;

    public int SuperApportionmentNumberOfMandatesForLotDecision { get; set; }

    public ICollection<DoubleProportionalResultRow> Rows { get; set; } = new HashSet<DoubleProportionalResultRow>();

    public ICollection<DoubleProportionalResultColumn> Columns { get; set; } = new HashSet<DoubleProportionalResultColumn>();
}
