// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public interface IHasResults
{
    IEnumerable<CountingCircleResult> Results { get; set; }
}
