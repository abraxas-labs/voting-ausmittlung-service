// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class CountingCircleResult : BaseEntity
{
    public Guid CountingCircleId { get; set; }

    public CountingCircle CountingCircle { get; set; } = null!;

    public abstract PoliticalBusiness PoliticalBusiness { get; }

    public abstract Guid PoliticalBusinessId { get; set; }

    public CountingCircleResultState State { get; set; } = CountingCircleResultState.Initial;

    /// <summary>
    /// Gets or sets the submission done timestamp.
    /// A submission done timestamp is set when the state is set to <see cref="CountingCircleResultState.SubmissionDone"/> or <see cref="CountingCircleResultState.CorrectionDone"/>.
    /// When a result is set to <see cref="CountingCircleResultState.ReadyForCorrection" /> this timestamp will be removed.
    /// </summary>
    public DateTime? SubmissionDoneTimestamp { get; set; }

    public DateTime? ReadyForCorrectionTimestamp { get; set; }

    public DateTime? AuditedTentativelyTimestamp { get; set; }

    public DateTime? PlausibilisedTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the total count of voters which is calculated from <see cref="ContestCountingCircleDetails"/>.
    /// In German: Anzahl Stimmberechtigte.
    /// </summary>
    public int TotalCountOfVoters { get; set; }

    /// <summary>
    /// Gets or sets the total sent e-voting voting cards which is set by the imported count of voters information.
    /// (Hint: the imported count of voters informations is a subset of the conventional submitted count of voters informations,
    /// which means that these dont need to be included in the <see cref="TotalCountOfVoters"/>).
    /// </summary>
    public int? TotalSentEVotingVotingCards { get; set; }

    public bool Published { get; set; }

    public bool SubmissionDone()
    {
        return State >= CountingCircleResultState.SubmissionDone;
    }
}
