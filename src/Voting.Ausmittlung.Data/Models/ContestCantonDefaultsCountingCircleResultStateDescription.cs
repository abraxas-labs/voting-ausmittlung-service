// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ContestCantonDefaultsCountingCircleResultStateDescription : BaseEntity
{
    public CountingCircleResultState State { get; set; }

    public string Description { get; set; } = string.Empty;
}
