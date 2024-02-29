// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingVotingCardImport
{
    public EVotingVotingCardImport(
        string echMessageId,
        Guid contestId,
        List<EVotingCountingCircleVotingCards> votingCards)
    {
        EchMessageId = echMessageId;
        ContestId = contestId;
        CountingCircleVotingCards = votingCards;
    }

    public string EchMessageId { get; }

    public Guid ContestId { get; }

    public List<EVotingCountingCircleVotingCards> CountingCircleVotingCards { get; set; }
}
