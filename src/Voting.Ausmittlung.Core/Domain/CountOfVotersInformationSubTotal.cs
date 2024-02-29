// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class CountOfVotersInformationSubTotal
{
    public SexType Sex { get; set; }

    public VoterType VoterType { get; set; }

    public int? CountOfVoters { get; set; }
}
