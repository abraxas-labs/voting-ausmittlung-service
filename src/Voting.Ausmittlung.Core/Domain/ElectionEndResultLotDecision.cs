// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

/// <summary>
/// When multiple candidates have the same rank in an election, a manual lot decision is made.
/// </summary>
public class ElectionEndResultLotDecision
{
    public Guid CandidateId { get; set; }

    public int Rank { get; set; }
}
