// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ProportionalElectionResultImport : ElectionResultImport
{
    private readonly Dictionary<Guid, ProportionalElectionCandidateResultImport> _candidateResults =
        new Dictionary<Guid, ProportionalElectionCandidateResultImport>();

    private readonly Dictionary<Guid, ProportionalElectionListResultImport> _listResults =
        new Dictionary<Guid, ProportionalElectionListResultImport>();

    public ProportionalElectionResultImport(Guid proportionalElectionId, Guid basisCountingCircleId)
        : base(proportionalElectionId, basisCountingCircleId)
    {
    }

    public Guid ProportionalElectionId => PoliticalBusinessId;

    public IEnumerable<ProportionalElectionCandidateResultImport> CandidateResults => _candidateResults.Values;

    public IEnumerable<ProportionalElectionListResultImport> ListResults => _listResults.Values;

    public int CountOfUnmodifiedLists { get; internal set; }

    public int CountOfModifiedLists { get; internal set; }

    public int CountOfListsWithoutParty { get; internal set; }

    public int CountOfBlankRowsOnListsWithoutParty { get; internal set; }

    internal ProportionalElectionCandidateResultImport GetOrAddCandidateResult(Guid candidateId)
        => _candidateResults.GetOrAdd(candidateId, () => new ProportionalElectionCandidateResultImport(candidateId));

    internal ProportionalElectionListResultImport GetOrAddListResult(Guid listId)
        => _listResults.GetOrAdd(listId, () => new ProportionalElectionListResultImport(listId));
}
