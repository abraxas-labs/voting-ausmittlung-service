// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models.Import;

public class MajorityElectionGroupedWriteInMappings
{
    public MajorityElectionGroupedWriteInMappings(
        Guid importId,
        ResultImportType importType,
        MajorityElectionBase election,
        IReadOnlyCollection<MajorityElectionWriteInMappingBase> writeInMappings)
    {
        ImportId = importId;
        ImportType = importType;
        Election = election;
        WriteInMappings = writeInMappings;
    }

    public Guid ImportId { get; }

    public ResultImportType ImportType { get; }

    public MajorityElectionBase Election { get; }

    public IReadOnlyCollection<MajorityElectionWriteInMappingBase> WriteInMappings { get; }
}
