// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionResult : BaseEntity,
    IHasSubTotals<MajorityElectionResultSubTotal, MajorityElectionResultNullableSubTotal>,
    IMajorityElectionResultTotal<int>
{
    public MajorityElectionResult PrimaryResult { get; set; } = null!;

    public Guid PrimaryResultId { get; set; }

    public SecondaryMajorityElection SecondaryMajorityElection { get; set; } = null!;

    public Guid SecondaryMajorityElectionId { get; set; }

    public ICollection<SecondaryMajorityElectionResultBallot> ResultBallots { get; set; }
        = new HashSet<SecondaryMajorityElectionResultBallot>();

    public ICollection<SecondaryMajorityElectionCandidateResult> CandidateResults { get; set; }
        = new HashSet<SecondaryMajorityElectionCandidateResult>();

    public ICollection<SecondaryMajorityElectionWriteInMapping> WriteInMappings { get; set; }
        = new HashSet<SecondaryMajorityElectionWriteInMapping>();

    public ICollection<SecondaryMajorityElectionWriteInBallot> WriteInBallots { get; set; }
        = new HashSet<SecondaryMajorityElectionWriteInBallot>();

    public MajorityElectionResultSubTotal EVotingSubTotal { get; set; } = new();

    public MajorityElectionResultNullableSubTotal ConventionalSubTotal { get; set; } = new();

    /// <inheritdoc />
    public int IndividualVoteCount => EVotingSubTotal.IndividualVoteCount + ConventionalSubTotal.IndividualVoteCount.GetValueOrDefault();

    /// <inheritdoc />
    public int EmptyVoteCount => EVotingSubTotal.EmptyVoteCountInclWriteIns + ConventionalSubTotal.EmptyVoteCountInclWriteIns.GetValueOrDefault();

    /// <inheritdoc />
    public int InvalidVoteCount => EVotingSubTotal.InvalidVoteCount + ConventionalSubTotal.InvalidVoteCount.GetValueOrDefault();

    /// <inheritdoc />
    public int TotalEmptyAndInvalidVoteCount => EVotingSubTotal.TotalEmptyAndInvalidVoteCount + ConventionalSubTotal.TotalEmptyAndInvalidVoteCount;

    /// <inheritdoc />
    public int TotalCandidateVoteCountExclIndividual => EVotingSubTotal.TotalCandidateVoteCountExclIndividual + ConventionalSubTotal.TotalCandidateVoteCountExclIndividual;

    /// <inheritdoc />
    public int TotalCandidateVoteCountInclIndividual => TotalCandidateVoteCountExclIndividual + IndividualVoteCount;

    public int TotalVoteCount => EVotingSubTotal.TotalVoteCount + ConventionalSubTotal.TotalVoteCount;

    public void ResetAllSubTotals(VotingDataSource dataSource, bool setZeroInsteadNull)
    {
        this.ResetSubTotal(dataSource, setZeroInsteadNull);

        foreach (var candidateResult in CandidateResults)
        {
            candidateResult.ResetSubTotal(dataSource, setZeroInsteadNull);
        }
    }
}
