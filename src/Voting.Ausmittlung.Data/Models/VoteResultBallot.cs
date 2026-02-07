// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class VoteResultBallot : BaseEntity
{
    public VoteResultBundle Bundle { get; set; } = null!;

    public Guid BundleId { get; set; }

    public ICollection<VoteResultBallotQuestionAnswer> QuestionAnswers { get; set; } = new HashSet<VoteResultBallotQuestionAnswer>();

    public ICollection<VoteResultBallotTieBreakQuestionAnswer> TieBreakQuestionAnswers { get; set; } = new HashSet<VoteResultBallotTieBreakQuestionAnswer>();

    public int Number { get; set; }

    public bool MarkedForReview { get; set; }

    /// <summary>
    /// Gets or sets the usually zero-based index. Older data may contain non-zero based numbers in here.
    /// Should only be used for ordering the ballot inside a bundle.
    /// </summary>
    public int Index { get; set; }

    public List<VoteResultBallotLog> Logs { get; set; } = new();
}
