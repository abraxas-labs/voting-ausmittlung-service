// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluencePartyTranslation : TranslationEntity
{
    public Guid DomainOfInfluencePartyId { get; set; }

    public DomainOfInfluenceParty? DomainOfInfluenceParty { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

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

        return Equals((DomainOfInfluencePartyTranslation)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Language,
            Name,
            ShortDescription);
    }

    private bool Equals(DomainOfInfluencePartyTranslation other)
    {
        return Language == other.Language
               && Name == other.Name
               && ShortDescription == other.ShortDescription;
    }
}
