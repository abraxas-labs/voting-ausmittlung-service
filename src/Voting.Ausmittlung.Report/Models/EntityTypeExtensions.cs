// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Report.Models;

internal static class EntityTypeExtensions
{
    internal static PoliticalBusinessType? ToPoliticalBusinessType(this EntityType type)
    {
        return type switch
        {
            EntityType.Vote => PoliticalBusinessType.Vote,
            EntityType.MajorityElection => PoliticalBusinessType.MajorityElection,
            EntityType.ProportionalElection => PoliticalBusinessType.ProportionalElection,
            _ => null,
        };
    }
}
