// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class CountingCircleElectorateBase : BaseEntity
{
    public List<DomainOfInfluenceType> DomainOfInfluenceTypes { get; set; } = new();
}
