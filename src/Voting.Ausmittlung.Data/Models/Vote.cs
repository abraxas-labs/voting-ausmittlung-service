// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class Vote : PoliticalBusiness, IHasResults, IPoliticalBusinessHasTranslations
{
    public ICollection<VoteResult> Results { get; set; } = new HashSet<VoteResult>();

    IEnumerable<CountingCircleResult> IHasResults.Results
    {
        get => Results;
        set => Results = value.Cast<VoteResult>().ToList();
    }

    public ICollection<Ballot> Ballots { get; set; } = new HashSet<Ballot>();

    public int ReportDomainOfInfluenceLevel { get; set; }

    public override PoliticalBusinessType BusinessType => PoliticalBusinessType.Vote;

    public VoteResultAlgorithm ResultAlgorithm { get; set; }

    public VoteEndResult? EndResult { get; set; }

    public ICollection<VoteTranslation> Translations { get; set; } = new HashSet<VoteTranslation>();

    IEnumerable<PoliticalBusinessTranslation> IPoliticalBusinessHasTranslations.Translations
    {
        get => Translations;
        set => Translations = value.Cast<VoteTranslation>().ToList();
    }

    public int BallotBundleSampleSizePercent { get; set; }

    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public bool EnforceResultEntryForCountingCircles { get; set; }

    public VoteResultEntry ResultEntry { get; set; }

    public VoteReviewProcedure ReviewProcedure { get; set; }

    public bool EnforceReviewProcedureForCountingCircles { get; set; }

    public string InternalDescription { get; set; } = string.Empty;
}
