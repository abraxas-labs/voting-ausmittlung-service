// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ImportMajorityElectionWriteInMappings
{
    internal ImportMajorityElectionWriteInMappings(Guid importId, IReadOnlyCollection<MajorityElectionGroupedWriteInMappings> electionWriteInMappings)
    {
        ImportId = importId;
        ElectionWriteInMappings = electionWriteInMappings;
    }

    public Guid ImportId { get; }

    public IReadOnlyCollection<MajorityElectionGroupedWriteInMappings> ElectionWriteInMappings { get; }
}
