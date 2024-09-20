// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public interface IEndResultDetail<TCountOfVotersInfo, TVotingCard>
    where TCountOfVotersInfo : EndResultCountOfVotersInformationSubTotal
    where TVotingCard : EndResultVotingCardDetail
{
    /// <summary>
    /// Gets or sets the count of voters information for this end result. This contains more detailed information about <see cref="TotalCountOfVoters"/>
    /// In German: Informationen über Stimmberechtigte.
    /// See <see cref="EndResultCountOfVotersInformationSubTotal"/> for more information.
    /// </summary>
    ICollection<TCountOfVotersInfo> CountOfVotersInformationSubTotals { get; set; }

    ICollection<TVotingCard> VotingCards { get; set; }
}
