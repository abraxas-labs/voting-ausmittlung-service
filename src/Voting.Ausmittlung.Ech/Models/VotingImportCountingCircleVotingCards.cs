// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Ech.Models;

public class VotingImportCountingCircleVotingCards
{
    public VotingImportCountingCircleVotingCards(string basisCountingCircleId, int countOfVotingCards)
    {
        BasisCountingCircleId = basisCountingCircleId;
        CountOfVotingCards = countOfVotingCards;
    }

    public string BasisCountingCircleId { get; }

    public int CountOfVotingCards { get; set; }
}
