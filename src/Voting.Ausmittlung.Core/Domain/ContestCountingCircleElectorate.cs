// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class ContestCountingCircleElectorate
{
    public Guid Id { get; set; }

    public IReadOnlyCollection<DomainOfInfluenceType> DomainOfInfluenceTypes { get; set; } = Array.Empty<DomainOfInfluenceType>();

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ContestCountingCircleElectorate)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, DomainOfInfluenceTypes.GetSequenceHashCode());
    }

    private bool Equals(ContestCountingCircleElectorate other)
    {
        return Id.Equals(other.Id) && DomainOfInfluenceTypes.SequenceEqual(other.DomainOfInfluenceTypes);
    }
}
