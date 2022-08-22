// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluencePermissionEntry : BaseEntity
{
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the basis domain of influence id (not snapshot id) of this permission entry.
    /// </summary>
    public Guid BasisDomainOfInfluenceId { get; set; } = Guid.Empty;

    public List<Guid> BasisCountingCircleIds { get; set; } = new();

    public List<Guid> CountingCircleIds { get; set; } = new();

    /// <summary>
    /// Gets or sets contest id of the contest, for which this entry is valid.
    /// </summary>
    public Guid ContestId { get; set; }

    public Contest? Contest { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry is final and therefore should never be modified anymore.
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the TenantId is only assigned on a child level or a counting circle.
    /// </summary>
    public bool IsParent { get; set; } = true;
}
