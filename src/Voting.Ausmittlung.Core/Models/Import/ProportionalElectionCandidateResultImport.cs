// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ProportionalElectionCandidateResultImport
{
    private readonly Dictionary<Guid, int> _voteSources = new Dictionary<Guid, int>();

    internal ProportionalElectionCandidateResultImport(Guid candidateId)
    {
        CandidateId = candidateId;
    }

    public Guid CandidateId { get; }

    public int VoteCount { get; internal set; }

    public int UnmodifiedListVotesCount { get; internal set; }

    public int ModifiedListVotesCount { get; internal set; }

    public int CountOfVotesOnOtherLists { get; internal set; }

    public int CountOfVotesFromAccumulations { get; internal set; }

    public IReadOnlyDictionary<Guid, int> VoteSources => _voteSources;

    public void AddVoteSourceVote(Guid? listId)
        => _voteSources.AddOrUpdate(listId ?? Guid.Empty, () => 1, v => v + 1);
}
