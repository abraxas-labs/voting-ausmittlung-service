// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResult : ElectionResult, IHasSubTotals<MajorityElectionResultSubTotal, MajorityElectionResultNullableSubTotal>, IMajorityElectionResultTotal<int>
{
    public Guid MajorityElectionId { get; set; }

    public MajorityElection MajorityElection { get; set; } = null!;

    public MajorityElectionResultEntry Entry { get; set; }

    public MajorityElectionResultEntryParams? EntryParams { get; set; }

    public ICollection<MajorityElectionCandidateResult> CandidateResults { get; set; } =
        new HashSet<MajorityElectionCandidateResult>();

    public MajorityElectionResultSubTotal EVotingSubTotal { get; set; } = new();

    public MajorityElectionResultNullableSubTotal ConventionalSubTotal { get; set; } = new();

    /// <summary>
    /// Gets or sets total count of ballot group votes.
    /// Only set if entry is detailed and only conventional ballot group votes are counted.
    /// Updated when the ballot group results are entered.
    /// </summary>
    public int ConventionalCountOfBallotGroupVotes { get; set; }

    /// <summary>
    /// Gets or sets total count of ballot group votes.
    /// Only set if entry is detailed and only conventional ballot group votes are counted.
    /// Updated when the ballot group results are entered.
    /// </summary>
    public int ConventionalCountOfDetailedEnteredBallots { get; set; }

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

    public ICollection<SecondaryMajorityElectionResult> SecondaryMajorityElectionResults { get; set; } =
        new HashSet<SecondaryMajorityElectionResult>();

    public ICollection<MajorityElectionBallotGroupResult> BallotGroupResults { get; set; } =
        new HashSet<MajorityElectionBallotGroupResult>();

    public ICollection<MajorityElectionResultBundle> Bundles { get; set; } =
        new HashSet<MajorityElectionResultBundle>();

    public ICollection<MajorityElectionWriteInMapping> WriteInMappings { get; set; }
        = new HashSet<MajorityElectionWriteInMapping>();

    public ICollection<MajorityElectionWriteInBallot> WriteInBallots { get; set; }
        = new HashSet<MajorityElectionWriteInBallot>();

    /// <summary>
    /// Gets a value indicating whether this result or any secondary result has write ins which are not mapped by a user yet.
    /// </summary>
    public bool HasUnmappedWriteIns => CountOfElectionsWithUnmappedWriteIns > 0;

    /// <summary>
    /// Gets or sets the count of elections with unmapped write ins.
    /// This includes the primary and all secondary elections.
    /// When this property is updated also update the same property in <see cref="SimpleCountingCircleResult"/>.
    /// </summary>
    public int CountOfElectionsWithUnmappedWriteIns { get; set; }

    public bool HasBallotGroups => BallotGroupResults.Count != 0;

    [NotMapped]
    public override PoliticalBusiness PoliticalBusiness => MajorityElection;

    [NotMapped]
    public override Guid PoliticalBusinessId
    {
        get => MajorityElectionId;
        set => MajorityElectionId = value;
    }

    public void ResetAllSubTotals(VotingDataSource dataSource, bool includeCountOfVoters = false)
    {
        var setZeroInsteadOfNull = Entry == MajorityElectionResultEntry.Detailed;

        this.ResetSubTotal(dataSource, setZeroInsteadOfNull);

        foreach (var candidateResult in CandidateResults)
        {
            candidateResult.ResetSubTotal(dataSource, setZeroInsteadOfNull);
        }

        foreach (var secondaryResult in SecondaryMajorityElectionResults)
        {
            secondaryResult.ResetAllSubTotals(dataSource, setZeroInsteadOfNull);
        }

        if (includeCountOfVoters)
        {
            CountOfVoters.ResetSubTotal(dataSource, TotalCountOfVoters);
        }

        if (dataSource == VotingDataSource.EVoting)
        {
            CountOfElectionsWithUnmappedWriteIns = 0;
        }
    }
}
