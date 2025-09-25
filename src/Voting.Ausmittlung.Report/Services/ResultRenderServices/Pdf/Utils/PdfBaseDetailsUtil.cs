// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfBaseDetailsUtil
{
    public static void FilterAndBuildVotingCardTotalsAndCountOfVoters<TPdfBaseDetails>(TPdfBaseDetails details, DomainOfInfluence domainOfInfluence)
        where TPdfBaseDetails : PdfBaseDetails
    {
        FilterAndBuildVotingCardTotals(details, domainOfInfluence.Type);
        FilterAndBuildCountOfVoters(details, domainOfInfluence);
    }

    public static void FilterAndBuildCountOfVoters<TPdfBaseDetails>(TPdfBaseDetails details, DomainOfInfluence domainOfInfluence)
        where TPdfBaseDetails : PdfBaseDetails
    {
        if (details.CountOfVotersInformationSubTotals == null)
        {
            details.TotalCountOfVoters = 0;
            return;
        }

        details.TotalCountOfVoters = GetTotalCountOfVotersForDomainOfInfluence(details, domainOfInfluence);

        var enabledVoterTypes = VoterTypesBuilder.BuildEnabledVoterTypes(domainOfInfluence);
        details.CountOfVotersInformationSubTotals = details.CountOfVotersInformationSubTotals
            .Where(s => s.DomainOfInfluenceType == domainOfInfluence.Type && enabledVoterTypes.Contains(s.VoterType))
            .ToList();
    }

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

    private static int GetTotalCountOfVotersForDomainOfInfluence<TPdfBaseDetails>(TPdfBaseDetails details, DomainOfInfluence domainOfInfluence)
        where TPdfBaseDetails : PdfBaseDetails
    {
        return details.CountOfVotersInformationSubTotals!
            .Where(x => x.DomainOfInfluenceType == domainOfInfluence.Type
                && (x.VoterType == VoterType.Swiss
                || (domainOfInfluence.SwissAbroadVotingRight == SwissAbroadVotingRight.OnEveryCountingCircle && x.VoterType == VoterType.SwissAbroad)
                || (domainOfInfluence.HasForeignerVoters && x.VoterType == VoterType.Foreigner)
                || (domainOfInfluence.HasMinorVoters && x.VoterType == VoterType.Minor)))
            .Sum(x => x.CountOfVoters);
    }
}
