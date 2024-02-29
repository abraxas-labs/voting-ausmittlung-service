// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public static class MajorityElectionWriteInMappingBaseCollectionExtensions
{
    public static bool HasUnspecifiedMappings(this IEnumerable<MajorityElectionWriteInMappingBase> mappings)
        => mappings.Any(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified);
}
