// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class AggregatedContestCountingCircleDetailsBuilder
{
    public static ContestDomainOfInfluenceDetails? BuildDomainOfInfluenceDetails(List<ContestCountingCircleDetails> ccDetails)
    {
        var details = new ContestDomainOfInfluenceDetails();

        foreach (var ccDetail in ccDetails)
        {
            AggregatedContestCountingCircleDetailsUtil.AdjustAggregatedDetails<ContestDomainOfInfluenceDetails, DomainOfInfluenceCountOfVotersInformationSubTotal, DomainOfInfluenceVotingCardResultDetail>(
                details,
                ccDetail,
                1);
        }

        return details;
    }

    public static ContestDetails? BuildContestDetails(List<ContestCountingCircleDetails> ccDetails)
    {
        var contestDetails = new ContestDetails();

        foreach (var ccDetail in ccDetails)
        {
            AggregatedContestCountingCircleDetailsUtil.AdjustAggregatedDetails<ContestDetails, ContestCountOfVotersInformationSubTotal, ContestVotingCardResultDetail>(
                contestDetails,
                ccDetail,
                1);
        }

        return contestDetails;
    }
}
