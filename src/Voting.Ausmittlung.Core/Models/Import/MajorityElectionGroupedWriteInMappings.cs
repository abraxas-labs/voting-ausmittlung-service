// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models.Import;

public class MajorityElectionGroupedWriteInMappings
{
    public MajorityElectionGroupedWriteInMappings(
        MajorityElectionBase election,
        IReadOnlyCollection<MajorityElectionWriteInMappingBase> writeInMappings)
    {
        Election = election;
        WriteInMappings = writeInMappings;
    }

    public MajorityElectionBase Election { get; }

    public IReadOnlyCollection<MajorityElectionWriteInMappingBase> WriteInMappings { get; }
}
