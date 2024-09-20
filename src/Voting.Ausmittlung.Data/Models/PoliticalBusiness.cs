// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class PoliticalBusiness : BaseEntity
{
    public string PoliticalBusinessNumber { get; set; } = string.Empty;

    public bool Active { get; set; }

    public virtual SwissAbroadVotingRight SwissAbroadVotingRight =>
        DomainOfInfluence?.SwissAbroadVotingRight ?? SwissAbroadVotingRight.Unspecified;

    public virtual Guid DomainOfInfluenceId { get; set; }

    public virtual DomainOfInfluence DomainOfInfluence { get; set; } = null!;

    public virtual Guid ContestId { get; set; }

    public virtual Contest Contest { get; set; } = null!;

    public virtual PoliticalBusinessType BusinessType { get; }

    public virtual PoliticalBusinessSubType BusinessSubType { get; }

    [NotMapped]
    public virtual IEnumerable<CountingCircleResult> CountingCircleResults
    {
        get => ((IHasResults)this).Results;
        set => ((IHasResults)this).Results = value;
    }

    [NotMapped]
    public virtual IEnumerable<PoliticalBusinessTranslation> PoliticalBusinessTranslations
    {
        get => ((IPoliticalBusinessHasTranslations)this).Translations;
        set => ((IPoliticalBusinessHasTranslations)this).Translations = value;
    }

    public string ShortDescription => PoliticalBusinessTranslations.GetTranslated(x => x.ShortDescription);

    public string OfficialDescription => PoliticalBusinessTranslations.GetTranslated(x => x.OfficialDescription);

    public string Title => $"{PoliticalBusinessNumber}: {ShortDescription}";
}
