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

    public MajorityElectionResultSubTotal ECountingSubTotal { get; set; } = new();

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
    public int IndividualVoteCount => EVotingSubTotal.IndividualVoteCount + ECountingSubTotal.IndividualVoteCount + ConventionalSubTotal.IndividualVoteCount.GetValueOrDefault();

    /// <inheritdoc />
    public int EmptyVoteCount => EVotingSubTotal.EmptyVoteCountInclWriteIns + ECountingSubTotal.EmptyVoteCountInclWriteIns + ConventionalSubTotal.EmptyVoteCountInclWriteIns.GetValueOrDefault();

    /// <inheritdoc />
    public int InvalidVoteCount => EVotingSubTotal.InvalidVoteCount + ECountingSubTotal.InvalidVoteCount + ConventionalSubTotal.InvalidVoteCount.GetValueOrDefault();

    /// <inheritdoc />
    public int TotalEmptyAndInvalidVoteCount => EVotingSubTotal.TotalEmptyAndInvalidVoteCount + ECountingSubTotal.TotalEmptyAndInvalidVoteCount + ConventionalSubTotal.TotalEmptyAndInvalidVoteCount;

    /// <inheritdoc />
    public int TotalCandidateVoteCountExclIndividual => EVotingSubTotal.TotalCandidateVoteCountExclIndividual + ECountingSubTotal.TotalCandidateVoteCountExclIndividual + ConventionalSubTotal.TotalCandidateVoteCountExclIndividual;

    /// <inheritdoc />
    public int TotalCandidateVoteCountInclIndividual => TotalCandidateVoteCountExclIndividual + IndividualVoteCount;

    public int TotalVoteCount => EVotingSubTotal.TotalVoteCount + ECountingSubTotal.TotalVoteCount + ConventionalSubTotal.TotalVoteCount;

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
    public bool HasUnmappedWriteIns => HasUnmappedEVotingWriteIns || HasUnmappedECountingWriteIns;

    public bool HasUnmappedEVotingWriteIns => CountOfElectionsWithUnmappedEVotingWriteIns > 0;

    public bool HasUnmappedECountingWriteIns => CountOfElectionsWithUnmappedECountingWriteIns > 0;

    /// <summary>
    /// Gets or sets the count of elections with unmapped e-voting write ins.
    /// This includes the primary and all secondary elections.
    /// When this property is updated also update the same property in <see cref="SimpleCountingCircleResult"/>.
    /// </summary>
    public int CountOfElectionsWithUnmappedEVotingWriteIns { get; set; }

    /// <summary>
    /// Gets or sets the count of elections with unmapped e-voting write ins.
    /// This includes the primary and all secondary elections.
    /// When this property is updated also update the same property in <see cref="SimpleCountingCircleResult"/>.
    /// </summary>
    public int CountOfElectionsWithUnmappedECountingWriteIns { get; set; }

    public bool HasBallotGroups => BallotGroupResults.Count != 0;

    [NotMapped]
    public override PoliticalBusiness PoliticalBusiness => MajorityElection;

    [NotMapped]
    public override Guid PoliticalBusinessId
    {
        get => MajorityElectionId;
        set => MajorityElectionId = value;
    }

    public void MoveECountingToConventional()
    {
        CountOfVoters.MoveECountingSubTotalsToConventional();
        this.MoveECountingSubTotalsToConventional();

        foreach (var result in CandidateResults)
        {
            result.MoveECountingToConventional();
        }

        foreach (var result in SecondaryMajorityElectionResults)
        {
            result.MoveECountingToConventional();
        }
    }

    public void ResetAllSubTotals(bool includeCountOfVoters = false)
    {
        foreach (var dataSource in Enum.GetValues<VotingDataSource>())
        {
            ResetAllSubTotals(dataSource, includeCountOfVoters);
        }
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

        switch (dataSource)
        {
            case VotingDataSource.EVoting:
                CountOfElectionsWithUnmappedEVotingWriteIns = 0;
                break;
            case VotingDataSource.ECounting:
                CountOfElectionsWithUnmappedECountingWriteIns = 0;
                break;
        }
    }

    public void RemoveElectionWithUnmappedWriteIns(VotingDataSource dataSource)
    {
        switch (dataSource)
        {
            case VotingDataSource.EVoting:
                CountOfElectionsWithUnmappedEVotingWriteIns--;
                break;
            case VotingDataSource.ECounting:
                CountOfElectionsWithUnmappedECountingWriteIns--;
                break;
        }
    }

    public void AddElectionWithUnmappedWriteIns(VotingDataSource dataSource)
    {
        switch (dataSource)
        {
            case VotingDataSource.EVoting:
                CountOfElectionsWithUnmappedEVotingWriteIns++;
                break;
            case VotingDataSource.ECounting:
                CountOfElectionsWithUnmappedECountingWriteIns++;
                break;
        }
    }
}
