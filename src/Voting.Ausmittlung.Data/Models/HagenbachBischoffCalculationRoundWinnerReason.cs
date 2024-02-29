// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum HagenbachBischoffCalculationRoundWinnerReason
{
    /// <summary>
    /// Sitz gewonnen durch grössten Quotient (Art 100 Abs. b https://www.gesetzessammlung.sg.ch/app/de/texts_of_law/125.3/versions/2500).
    /// </summary>
    Quotient,

    /// <summary>
    /// Sitz gewonnen durch grössten Rest-Quotient (Art 100 Abs. c https://www.gesetzessammlung.sg.ch/app/de/texts_of_law/125.3/versions/2500).
    /// </summary>
    QuotientRemainder,

    /// <summary>
    /// Sitz gewonnen durch grösste Anz. Stimmen (Art 100 Abs. d https://www.gesetzessammlung.sg.ch/app/de/texts_of_law/125.3/versions/2500).
    /// </summary>
    VoteCount,
}
