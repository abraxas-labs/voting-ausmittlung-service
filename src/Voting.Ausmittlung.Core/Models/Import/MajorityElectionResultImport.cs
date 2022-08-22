// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models.Import;

public class MajorityElectionResultImport : ElectionResultImport
{
    internal static readonly IEqualityComparer<string> WriteInComparer = StringComparer.OrdinalIgnoreCase; // write ins should be case-insensitive

    private readonly Dictionary<Guid, int> _candidateVoteCounts = new();
    private readonly Dictionary<string, int> _writeInVoteCounts = new(WriteInComparer);

    public MajorityElectionResultImport(
        Guid politicalBusinessId,
        Guid basisCountingCircleId)
        : base(politicalBusinessId, basisCountingCircleId)
    {
    }

    public int InvalidVoteCount { get; internal set; }

    public int EmptyVoteCount { get; internal set; }

    public int TotalCandidateVoteCountExclIndividual { get; internal set; }

    public IReadOnlyDictionary<Guid, int> CandidateVoteCounts => _candidateVoteCounts;

    public IReadOnlyDictionary<string, int> WriteInVoteCounts => _writeInVoteCounts;

    internal void AddCandidateVote(Guid candidateId)
    {
        _candidateVoteCounts.AddOrUpdate(candidateId, () => 1, i => i + 1);
        TotalCandidateVoteCountExclIndividual++;
    }

    internal void AddInvalidOrEmptyVote(bool supportsInvalidVotes)
    {
        if (supportsInvalidVotes)
        {
            InvalidVoteCount++;
        }
        else
        {
            EmptyVoteCount++;
        }
    }

    internal void AddMissingWriteIn(string name)
        => _writeInVoteCounts.AddOrUpdate(name, () => 1, i => i + 1);
}
