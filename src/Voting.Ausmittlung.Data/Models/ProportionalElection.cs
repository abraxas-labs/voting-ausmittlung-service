// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElection : Election, IHasResults, IPoliticalBusinessHasTranslations
{
    public ProportionalElectionMandateAlgorithm MandateAlgorithm { get; set; }

    public bool CandidateCheckDigit { get; set; }

    public int BallotBundleSize { get; set; }

    public int BallotBundleSampleSize { get; set; }

    public bool AutomaticBallotBundleNumberGeneration { get; set; }

    public bool AutomaticBallotNumberGeneration { get; set; }

    public BallotNumberGeneration BallotNumberGeneration { get; set; }

    public bool AutomaticEmptyVoteCounting { get; set; }

    public bool EnforceEmptyVoteCountingForCountingCircles { get; set; }

    public override PoliticalBusinessType BusinessType => PoliticalBusinessType.ProportionalElection;

    public ICollection<ProportionalElectionList> ProportionalElectionLists { get; set; } = new HashSet<ProportionalElectionList>();

    public ICollection<ProportionalElectionListUnion> ProportionalElectionListUnions { get; set; } = new HashSet<ProportionalElectionListUnion>();

    public ICollection<ProportionalElectionResult> Results { get; set; } = new HashSet<ProportionalElectionResult>();

    public ICollection<ProportionalElectionUnionEntry> ProportionalElectionUnionEntries { get; set; } = new HashSet<ProportionalElectionUnionEntry>();

    public ProportionalElectionEndResult? EndResult { get; set; }

    public DoubleProportionalResult? DoubleProportionalResult { get; set; }

    public ICollection<DoubleProportionalResultRow> DoubleProportionalResultRows { get; set; } = new List<DoubleProportionalResultRow>();

    IEnumerable<CountingCircleResult> IHasResults.Results
    {
        get => Results;
        set => Results = value.Cast<ProportionalElectionResult>().ToList();
    }

    public ICollection<ProportionalElectionTranslation> Translations { get; set; } = new HashSet<ProportionalElectionTranslation>();

    IEnumerable<PoliticalBusinessTranslation> IPoliticalBusinessHasTranslations.Translations
    {
        get => Translations;
        set => Translations = value.Cast<ProportionalElectionTranslation>().ToList();
    }

    public ProportionalElectionReviewProcedure ReviewProcedure { get; set; }

    public bool EnforceReviewProcedureForCountingCircles { get; set; }

    public bool EnforceCandidateCheckDigitForCountingCircles { get; set; }

    public int? FederalIdentification { get; set; }

    public override void MoveECountingToConventional()
    {
        EndResult?.MoveECountingToConventional();

        foreach (var result in Results)
        {
            result.MoveECountingToConventional();
        }
    }
}
