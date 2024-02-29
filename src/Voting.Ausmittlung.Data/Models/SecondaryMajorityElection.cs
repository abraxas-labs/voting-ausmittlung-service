// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElection : Election, IPoliticalBusinessHasTranslations
{
    public SecondaryMajorityElectionAllowedCandidate AllowedCandidates { get; set; }

    public Guid PrimaryMajorityElectionId { get; set; }

    public MajorityElection PrimaryMajorityElection { get; set; } = null!; // set by EF

    public override PoliticalBusinessType BusinessType => PoliticalBusinessType.SecondaryMajorityElection;

    public Guid ElectionGroupId { get; set; }

    public ElectionGroup ElectionGroup { get; set; } = null!; // set by EF

    public ICollection<SecondaryMajorityElectionCandidate> Candidates { get; set; }
        = new HashSet<SecondaryMajorityElectionCandidate>();

    public ICollection<MajorityElectionBallotGroupEntry> BallotGroupEntries { get; set; }
        = new HashSet<MajorityElectionBallotGroupEntry>();

    public ICollection<SecondaryMajorityElectionResult> Results { get; set; }
        = new HashSet<SecondaryMajorityElectionResult>();

    public SecondaryMajorityElectionEndResult? EndResult { get; set; }

    public ICollection<SecondaryMajorityElectionTranslation> Translations { get; set; } = new HashSet<SecondaryMajorityElectionTranslation>();

    IEnumerable<PoliticalBusinessTranslation> IPoliticalBusinessHasTranslations.Translations
    {
        get => Translations;
        set => Translations = value.Cast<SecondaryMajorityElectionTranslation>().ToList();
    }

    public override SwissAbroadVotingRight SwissAbroadVotingRight
    {
        get => PrimaryMajorityElection.SwissAbroadVotingRight;
    }

    public override Guid DomainOfInfluenceId
    {
        get => PrimaryMajorityElection.DomainOfInfluenceId;
        set => throw new InvalidOperationException($"{nameof(DomainOfInfluenceId)} is read only.");
    }

    public override DomainOfInfluence DomainOfInfluence
    {
        get => PrimaryMajorityElection.DomainOfInfluence;
        set => throw new InvalidOperationException($"{nameof(DomainOfInfluence)} is read only.");
    }

    public override Guid ContestId
    {
        get => PrimaryMajorityElection.ContestId;
        set => throw new InvalidOperationException($"{nameof(ContestId)} is read only.");
    }

    public override Contest Contest
    {
        get => PrimaryMajorityElection.Contest;
        set => throw new InvalidOperationException($"{nameof(Contest)} is read only.");
    }

    public override IEnumerable<CountingCircleResult> CountingCircleResults
    {
        get => Enumerable.Empty<CountingCircleResult>();
        set => throw new InvalidOperationException($"{nameof(CountingCircleResults)} is read only.");
    }
}
