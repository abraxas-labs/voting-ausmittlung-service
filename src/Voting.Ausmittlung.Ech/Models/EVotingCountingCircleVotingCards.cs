// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingCountingCircleVotingCards
{
    public EVotingCountingCircleVotingCards(string basisCountingCircleId, int countOfVotingCards)
    {
        BasisCountingCircleId = basisCountingCircleId;
        CountOfVotingCards = countOfVotingCards;
    }

    public string BasisCountingCircleId { get; }

    public int CountOfVotingCards { get; set; }
}
