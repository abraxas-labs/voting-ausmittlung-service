// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// The count of voters information for an end result.
/// The count of voters for an end result may not match the count of voters of the contest (see <see cref="CountOfVotersInformationSubTotal"/>),
/// because the count of voters only count towards an end result if the linked counting circle is in a certain state.
/// </summary>
public abstract class EndResultCountOfVotersInformationSubTotal : BaseEntity
{
    public SexType Sex { get; set; } = SexType.Undefined;

    public int? CountOfVoters { get; set; }

    public VoterType VoterType { get; set; }
}
