// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElection : MajorityElectionBase, IHasResults, IPoliticalBusinessHasTranslations
{
    public MajorityElectionMandateAlgorithm MandateAlgorithm { get; set; }

    public bool CandidateCheckDigit { get; set; }

    public int BallotBundleSize { get; set; }

    public int BallotBundleSampleSize { get; set; }

    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public BallotNumberGeneration BallotNumberGeneration { get; set; }

    public bool AutomaticEmptyVoteCounting { get; set; }

    public bool EnforceEmptyVoteCountingForCountingCircles { get; set; }

    public MajorityElectionResultEntry ResultEntry { get; set; }

    public bool EnforceResultEntryForCountingCircles { get; set; }

    public int ReportDomainOfInfluenceLevel { get; set; }

    public override PoliticalBusinessType BusinessType => PoliticalBusinessType.MajorityElection;

    public ICollection<MajorityElectionCandidate> MajorityElectionCandidates { get; set; } = new HashSet<MajorityElectionCandidate>();

    public ICollection<SecondaryMajorityElection> SecondaryMajorityElections { get; set; } = new HashSet<SecondaryMajorityElection>();

    public ElectionGroup? ElectionGroup { get; set; }

    public ICollection<MajorityElectionBallotGroup> BallotGroups { get; set; } = new HashSet<MajorityElectionBallotGroup>();

    public ICollection<MajorityElectionBallotGroupEntry> BallotGroupEntries { get; set; } = new HashSet<MajorityElectionBallotGroupEntry>();

    public ICollection<MajorityElectionResult> Results { get; set; } = new HashSet<MajorityElectionResult>();

    public ICollection<MajorityElectionUnionEntry> MajorityElectionUnionEntries { get; set; } = new HashSet<MajorityElectionUnionEntry>();

    public MajorityElectionEndResult? EndResult { get; set; }

    IEnumerable<CountingCircleResult> IHasResults.Results
    {
        get => Results;
        set => Results = value.Cast<MajorityElectionResult>().ToList();
    }

    public ICollection<MajorityElectionTranslation> Translations { get; set; } = new HashSet<MajorityElectionTranslation>();

    IEnumerable<PoliticalBusinessTranslation> IPoliticalBusinessHasTranslations.Translations
    {
        get => Translations;
        set => Translations = value.Cast<MajorityElectionTranslation>().ToList();
    }

    public MajorityElectionReviewProcedure ReviewProcedure { get; set; }

    public bool EnforceReviewProcedureForCountingCircles { get; set; }

    public bool EnforceCandidateCheckDigitForCountingCircles { get; set; }

    public int? FederalIdentification { get; set; }

    /// <summary>
    /// Gets or sets the id of the primary election.
    /// Can only be set if this is a secondary election on a separate ballot.
    /// </summary>
    public Guid? PrimaryMajorityElectionId { get; set; }

    public MajorityElection? PrimaryMajorityElection { get; set; }

    public ICollection<MajorityElection> SecondaryMajorityElectionsOnSeparateBallots { get; set; } =
        new HashSet<MajorityElection>();
}
