// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class ContestDomainOfInfluenceDetails
{
    // used to map back to counting circles
    public Guid DomainOfInfluenceId { get; set; }

    public int TotalCountOfVoters { get; set; }

    public int TotalCountOfValidVotingCards { get; set; }

    public int TotalCountOfInvalidVotingCards { get; set; }

    public int TotalCountOfVotingCards => TotalCountOfValidVotingCards + TotalCountOfInvalidVotingCards;
}
