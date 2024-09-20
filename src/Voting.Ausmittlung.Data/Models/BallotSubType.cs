// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum BallotSubType
{
    /// <summary>
    /// Ballot sub type is unspecified, which is the case when the VoteType is QuestionsOnSingleBallot.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The ballot contains the main ballot question.
    /// </summary>
    MainBallot,

    /// <summary>
    /// The ballot contains the first counter proposal question.
    /// </summary>
    CounterProposal1,

    /// <summary>
    /// The ballot contains the main ballot question.
    /// </summary>
    CounterProposal2,

    /// <summary>
    /// The ballot contains the main ballot question.
    /// </summary>
    Variant1,

    /// <summary>
    /// The ballot contains the main ballot question.
    /// </summary>
    Variant2,

    /// <summary>
    /// The ballot contains the main ballot question.
    /// </summary>
    TieBreak1,

    /// <summary>
    /// The ballot contains the main ballot question.
    /// </summary>
    TieBreak2,

    /// <summary>
    /// The ballot contains the main ballot question.
    /// </summary>
    TieBreak3,
}
