// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public abstract class ElectionEndResultAvailableLotDecision<TCandidate>
    where TCandidate : ElectionCandidate
{
    public TCandidate Candidate { get; internal set; } = null!; // set by builder

    public int VoteCount { get; internal set; }

    public bool LotDecisionRequired { get; internal set; }

    public List<int> SelectableRanks { get; internal set; } = null!; // set by builder

    public int OriginalRank { get; internal set; }

    public int? SelectedRank { get; internal set; }
}
