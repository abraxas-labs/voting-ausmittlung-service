// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum VoteType
{
    /// <summary>
    /// The default value for votes which existed before this type was introduced.
    /// </summary>
    Unspecified,

    /// <summary>
    /// All questions (whether a single one in a standard ballot or multiple in a variant ballot) are on the same ballot.
    /// </summary>
    QuestionsOnSingleBallot,

    /// <summary>
    /// Each ballot contains a single question (main ballot, counter proposal, ...), which form a variant vote together.
    /// </summary>
    VariantQuestionsOnMultipleBallots,
}
