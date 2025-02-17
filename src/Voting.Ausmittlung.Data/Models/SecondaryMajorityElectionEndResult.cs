﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionEndResult : BaseEntity, IHasSubTotals<MajorityElectionResultSubTotal>, IMajorityElectionResultTotal<int>
{
    public Guid SecondaryMajorityElectionId { get; set; }

    public SecondaryMajorityElection SecondaryMajorityElection { get; set; } = null!; // set by ef

    public Guid PrimaryMajorityElectionEndResultId { get; set; }

    public MajorityElectionEndResult PrimaryMajorityElectionEndResult { get; set; } = null!; // set by ef

    public MajorityElectionResultSubTotal EVotingSubTotal { get; set; } = new();

    public MajorityElectionResultSubTotal ConventionalSubTotal { get; set; } = new();

    public ICollection<SecondaryMajorityElectionCandidateEndResult> CandidateEndResults { get; set; }
        = new HashSet<SecondaryMajorityElectionCandidateEndResult>();

    public MajorityElectionEndResultCalculation Calculation { get; set; } = new();

    /// <inheritdoc />
    public int IndividualVoteCount => EVotingSubTotal.IndividualVoteCount + ConventionalSubTotal.IndividualVoteCount;

    /// <inheritdoc />
    public int EmptyVoteCount => EVotingSubTotal.EmptyVoteCountInclWriteIns + ConventionalSubTotal.EmptyVoteCountInclWriteIns;

    /// <inheritdoc />
    public int InvalidVoteCount => EVotingSubTotal.InvalidVoteCount + ConventionalSubTotal.InvalidVoteCount;

    /// <inheritdoc />
    public int TotalEmptyAndInvalidVoteCount => EVotingSubTotal.TotalEmptyAndInvalidVoteCount + ConventionalSubTotal.TotalEmptyAndInvalidVoteCount;

    /// <inheritdoc />
    public int TotalCandidateVoteCountExclIndividual => EVotingSubTotal.TotalCandidateVoteCountExclIndividual + ConventionalSubTotal.TotalCandidateVoteCountExclIndividual;

    /// <inheritdoc />
    public int TotalCandidateVoteCountInclIndividual => TotalCandidateVoteCountExclIndividual + IndividualVoteCount;

    public int TotalVoteCount => EVotingSubTotal.TotalVoteCount + ConventionalSubTotal.TotalVoteCount;

    public void ResetAllSubTotals(VotingDataSource dataSource)
    {
        this.ResetSubTotal(dataSource);

        foreach (var candidateEndResult in CandidateEndResults)
        {
            candidateEndResult.ResetSubTotal(dataSource);
        }
    }
}
