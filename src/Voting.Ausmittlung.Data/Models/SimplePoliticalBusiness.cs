// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// This is an entity which provides simpler access to political businesses of all types.
/// </summary>
public class SimplePoliticalBusiness : PoliticalBusiness, IPoliticalBusinessHasTranslations
{
    public override PoliticalBusinessType BusinessType => PoliticalBusinessType;

    public PoliticalBusinessType PoliticalBusinessType { get; set; }

    public ICollection<SimpleCountingCircleResult> SimpleResults { get; set; } = new HashSet<SimpleCountingCircleResult>();

    /// <summary>
    /// Gets or sets the number of mandates.
    /// Only set for businesses of <see cref="BusinessType"/> election (<see cref="PoliticalBusinessType"/>).
    /// This redundancy is introduced to reduce required joins to resolve data and therefore reduce query times.
    /// </summary>
    public int? NumberOfMandates { get; set; }

    public int CountOfSecondaryBusinesses { get; set; }

    public override IEnumerable<CountingCircleResult> CountingCircleResults
    {
        get => Array.Empty<CountingCircleResult>();
        set => throw new InvalidOperationException("not available for simple political businesses");
    }

    public bool EndResultFinalized { get; set; }

    public ICollection<SimplePoliticalBusinessTranslation> Translations { get; set; } = new HashSet<SimplePoliticalBusinessTranslation>();

    public ICollection<ResultExportConfigurationPoliticalBusiness> ResultExportConfigurations { get; set; } = new HashSet<ResultExportConfigurationPoliticalBusiness>();

    IEnumerable<PoliticalBusinessTranslation> IPoliticalBusinessHasTranslations.Translations
    {
        get => Translations;
        set => Translations = value.Cast<SimplePoliticalBusinessTranslation>().ToList();
    }
}
