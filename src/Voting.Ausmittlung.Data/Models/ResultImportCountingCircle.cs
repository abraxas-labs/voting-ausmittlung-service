// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ResultImportCountingCircle : BaseEntity
{
    public ResultImport? ResultImport { get; set; }

    public Guid ResultImportId { get; set; }

    public CountingCircle? CountingCircle { get; set; }

    public Guid CountingCircleId { get; set; }
}
