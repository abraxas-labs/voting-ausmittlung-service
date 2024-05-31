// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum BallotQuestionType
{
    /// <summary>
    /// Ballot question type is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Main ballot (Hauptvorlage).
    /// </summary>
    MainBallot,

    /// <summary>
    /// Counter proposal (Gegenvorschlag).
    /// </summary>
    CounterProposal,

    /// <summary>
    /// Variant (Variante).
    /// </summary>
    Variant,
}
