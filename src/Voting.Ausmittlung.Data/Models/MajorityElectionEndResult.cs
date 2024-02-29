// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionEndResult : PoliticalBusinessEndResultBase,
    IEndResultDetail<MajorityElectionEndResultCountOfVotersInformationSubTotal, MajorityElectionEndResultVotingCardDetail>,
    IHasSubTotals<MajorityElectionResultSubTotal>,
    IMajorityElectionResultTotal<int>
{
    public Guid MajorityElectionId { get; set; }

    public MajorityElection MajorityElection { get; set; } = null!;

    public ICollection<MajorityElectionEndResultCountOfVotersInformationSubTotal> CountOfVotersInformationSubTotals { get; set; }
        = new HashSet<MajorityElectionEndResultCountOfVotersInformationSubTotal>();

    public ICollection<MajorityElectionEndResultVotingCardDetail> VotingCards { get; set; }
        = new HashSet<MajorityElectionEndResultVotingCardDetail>();

    /// <summary>
    /// Gets or sets the count of voters, meaning the persons who actually voted in this election.
    /// In German: Wahlzettel.
    /// </summary>
    public PoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new();

    public MajorityElectionEndResultCalculation Calculation { get; set; } = new();

    public ICollection<MajorityElectionCandidateEndResult> CandidateEndResults { get; set; }
        = new HashSet<MajorityElectionCandidateEndResult>();

    public ICollection<SecondaryMajorityElectionEndResult> SecondaryMajorityElectionEndResults { get; set; }
        = new HashSet<SecondaryMajorityElectionEndResult>();

    public MajorityElectionResultSubTotal EVotingSubTotal { get; set; } = new();

    public MajorityElectionResultSubTotal ConventionalSubTotal { get; set; } = new();

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

    [NotMapped]
    public IEnumerable<MajorityElectionCandidateEndResultBase> PrimaryAndSecondaryCandidateEndResults =>
        CandidateEndResults
            .Cast<MajorityElectionCandidateEndResultBase>()
            .Concat(SecondaryMajorityElectionEndResults.SelectMany(x => x.CandidateEndResults));

    public void ResetAllSubTotals(VotingDataSource dataSource, bool includeCountOfVoters = false)
    {
        this.ResetSubTotal(dataSource);

        foreach (var candidateEndResult in CandidateEndResults)
        {
            candidateEndResult.ResetSubTotal(dataSource);
        }

        foreach (var secondaryEndResult in SecondaryMajorityElectionEndResults)
        {
            secondaryEndResult.ResetAllSubTotals(dataSource);
        }

        if (includeCountOfVoters)
        {
            CountOfVoters.ResetSubTotal(dataSource, TotalCountOfVoters);
        }
    }

    public void ResetCalculation()
    {
        Calculation.AbsoluteMajority = null;
    }
}
