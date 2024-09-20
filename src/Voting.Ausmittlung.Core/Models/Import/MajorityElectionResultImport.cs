// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models.Import;

public class MajorityElectionResultImport : ElectionResultImport
{
    internal static readonly IEqualityComparer<string> WriteInComparer = StringComparer.OrdinalIgnoreCase; // write ins should be case-insensitive

    private readonly Dictionary<Guid, int> _candidateVoteCounts = new();
    private readonly Dictionary<string, WriteInMapping> _writeIns = new(WriteInComparer);
    private readonly List<MajorityElectionBallot> _writeInBallots = new();

    public MajorityElectionResultImport(
        Guid politicalBusinessId,
        Guid basisCountingCircleId,
        CountingCircleResultCountOfVotersInformationImport countOfVotersInformationImport)
        : base(politicalBusinessId, basisCountingCircleId, countOfVotersInformationImport)
    {
    }

    /// <summary>
    /// Gets the count of blank ballots, which only contain empty votes.
    /// The empty votes of the blank ballot are not counted.
    /// </summary>
    public int BlankBallotCount { get; internal set; }

    public int InvalidVoteCount { get; internal set; }

    public int EmptyVoteCount { get; internal set; }

    public int TotalCandidateVoteCountExclIndividual { get; internal set; }

    public IReadOnlyDictionary<Guid, int> CandidateVoteCounts => _candidateVoteCounts;

    /// <summary>
    /// Gets the aggregated write ins over the whole election by (case insensitive) name.
    /// </summary>
    public IReadOnlyDictionary<string, WriteInMapping> WriteIns => _writeIns;

    /// <summary>
    /// Gets the ballots which contain a write in.
    /// We need to keep track of them, since mapping the write ins needs information of the whole ballot.
    /// For example, a write in may be mapped to a candidate that already exists on the ballot.
    /// Two write ins on the same ballot may also be mapped to the same candidate.
    /// </summary>
    public IReadOnlyList<MajorityElectionBallot> WriteInBallots => _writeInBallots;

    internal void AddBallot(MajorityElectionBallot ballot)
    {
        // If all positions are empty, treat the whole ballot as blank and ignore the ballot positions
        // Note: This does not work correctly with secondary majority elections,
        // as we would need to check whether all positions on the secondary election are also empty
        if (ballot.CandidateIds.Count == 0 && ballot.WriteIns.Count == 0 && ballot.InvalidVoteCount == 0)
        {
            BlankBallotCount++;
            return;
        }

        foreach (var candidateId in ballot.CandidateIds)
        {
            _candidateVoteCounts.AddOrUpdate(candidateId, () => 1, i => i + 1);
            TotalCandidateVoteCountExclIndividual++;
        }

        foreach (var writeIn in ballot.WriteIns)
        {
            var mapping = _writeIns.AddOrUpdate(
                writeIn,
                () => new WriteInMapping(writeIn, 1),
                writeInMapping =>
                {
                    writeInMapping.CountOfVotes++;
                    return writeInMapping;
                });
            ballot.WriteInMappingIds.Add(mapping.Id);
        }

        if (ballot.WriteIns.Count > 0)
        {
            _writeInBallots.Add(ballot);
        }

        EmptyVoteCount += ballot.EmptyVoteCount;
        InvalidVoteCount += ballot.InvalidVoteCount;
    }
}
