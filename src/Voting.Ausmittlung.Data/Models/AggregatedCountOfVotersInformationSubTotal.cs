// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class AggregatedCountOfVotersInformationSubTotal : BaseEntity
{
    public SexType Sex { get; set; } = SexType.Undefined;

    public int CountOfVoters { get; set; }

    public VoterType VoterType { get; set; }
}
