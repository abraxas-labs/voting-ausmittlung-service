// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ComparisonVoterParticipationConfiguration : BaseEntity
{
    public DomainOfInfluenceType MainLevel { get; set; }

    public DomainOfInfluenceType ComparisonLevel { get; set; }

    public decimal? ThresholdPercent { get; set; }

    public PlausibilisationConfiguration PlausibilisationConfiguration { get; set; } = null!;

    public Guid PlausibilisationConfigurationId { get; set; }

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

        return Equals((ComparisonVoterParticipationConfiguration)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            MainLevel,
            ComparisonLevel,
            ThresholdPercent);
    }

    private bool Equals(ComparisonVoterParticipationConfiguration other)
    {
        return MainLevel.Equals(other.MainLevel)
               && ComparisonLevel.Equals(other.ComparisonLevel)
               && ThresholdPercent.Equals(other.ThresholdPercent);
    }
}
