// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfContestCountingCircleDetailsUtil
{
    public static void FilterAndBuildVotingCardTotals(PdfContestCountingCircleDetails details, DomainOfInfluenceType domainOfInfluenceType)
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

        details.TotalCountOfValidVotingCards = countOfValidCards;
        details.TotalCountOfInvalidVotingCards = countOfInvalidCards;
        details.TotalCountOfVotingCards = countOfValidCards + countOfInvalidCards;
    }
}
