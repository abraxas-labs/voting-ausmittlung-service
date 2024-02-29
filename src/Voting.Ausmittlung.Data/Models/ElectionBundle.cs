// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public abstract class ElectionBundle<TResult> : PoliticalBusinessBundle
    where TResult : ElectionResult
{
    public TResult ElectionResult { get; set; } = null!;

    public Guid ElectionResultId { get; set; }
}
