// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluenceCountingCircle : BaseEntity
{
    public Guid DomainOfInfluenceId { get; set; }

    public DomainOfInfluence DomainOfInfluence { get; set; } = null!; // set by ef

    public Guid CountingCircleId { get; set; }

    public CountingCircle CountingCircle { get; set; } = null!; // set by ef

    public ComparisonCountOfVotersCategory ComparisonCountOfVotersCategory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the relation is inherited by a Child DomainOfInfluence or not.
    /// </summary>
    public bool Inherited { get; set; }
}
