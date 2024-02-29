// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfBaseDetailsUtil
{
    public static void FilterAndBuildVotingCardTotals<TPdfBaseDetails>(TPdfBaseDetails details, DomainOfInfluenceType domainOfInfluenceType)
        where TPdfBaseDetails : PdfBaseDetails
    {
        if (details.VotingCards == null)
        {
            return;
        }

        details.VotingCards = details.VotingCards.Where(x => x.DomainOfInfluenceType == domainOfInfluenceType).ToList();

        var countOfValidCards = details.VotingCards
            .Where(v => v.Valid)
            .Sum(v => v.CountOfReceivedVotingCards);

        var countOfInvalidCards = details.VotingCards
            .Where(v => !v.Valid)
            .Sum(v => v.CountOfReceivedVotingCards);

        var eVotingVotingCard = details.VotingCards.SingleOrDefault(v => v.Channel == PdfVotingChannel.EVoting);

        details.TotalCountOfValidVotingCards = countOfValidCards;
        details.TotalCountOfInvalidVotingCards = countOfInvalidCards;
        details.TotalCountOfVotingCards = countOfValidCards + countOfInvalidCards;
        details.TotalCountOfEVotingVotingCards = eVotingVotingCard?.CountOfReceivedVotingCards ?? 0;
    }
}
