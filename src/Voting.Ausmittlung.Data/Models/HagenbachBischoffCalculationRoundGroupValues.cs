// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// This represents the values used and the results of an iteration of
/// Art. 100 from the hagenbach bischoff calculation
/// (see <a href="https://www.gesetzessammlung.sg.ch/app/de/texts_of_law/125.3/versions/2500">here</a>).
/// </summary>
public class HagenbachBischoffCalculationRoundGroupValues : BaseEntity
{
    public HagenbachBischoffGroup? Group { get; set; }

    public Guid GroupId { get; set; }

    public HagenbachBischoffCalculationRound? CalculationRound { get; set; }

    public Guid CalculationRoundId { get; set; }

    public decimal NextQuotient { get; set; }

    public decimal PreviousQuotient { get; set; }

    public int NumberOfMandates { get; set; }

    public int PreviousNumberOfMandates { get; set; }

    public bool IsWinner { get; set; }
}
