// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// This represents an iteration of
/// Art. 100 from the hagenbach bischoff calculation
/// (see <a href="https://www.gesetzessammlung.sg.ch/app/de/texts_of_law/125.3/versions/2500">here</a>).
/// </summary>
public class HagenbachBischoffCalculationRound : BaseEntity
{
    public HagenbachBischoffGroup Group { get; set; } = null!; // set by ef

    public Guid GroupId { get; set; }

    public int Index { get; set; }

    public HagenbachBischoffGroup Winner { get; set; } = null!;

    public Guid WinnerId { get; set; }

    public HagenbachBischoffCalculationRoundWinnerReason WinnerReason { get; set; }

    public ICollection<HagenbachBischoffCalculationRoundGroupValues> GroupValues { get; set; }
        = new HashSet<HagenbachBischoffCalculationRoundGroupValues>();
}
