// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Ech.Models;

public class VotingImport
{
    private readonly Dictionary<(Guid PoliticalBusinessId, string BasisCountingCircleId), VotingImportPoliticalBusinessResult> _politicalBusinessResults = new();

    public VotingImport(
        string echMessageId,
        Guid contestId)
    {
        EchMessageId = echMessageId;
        ContestId = contestId;
    }

    public string EchMessageId { get; private set; }

    public Guid ContestId { get; }

    public IReadOnlyCollection<VotingImportPoliticalBusinessResult> PoliticalBusinessResults => _politicalBusinessResults.Values;

    public List<VotingImportCountingCircleVotingCards>? VotingCards { get; set; }

    public void AddEchMessageId(string id)
    {
        EchMessageId += " / " + id;
    }

    public void AddEmptyResult(string countingCircleId)
        => AddResults([new VotingImportEmptyResult(countingCircleId)]);

    public void AddResults(IEnumerable<VotingImportPoliticalBusinessResult> results)
    {
        foreach (var result in results)
        {
            if (!_politicalBusinessResults.TryAdd((result.PoliticalBusinessId, result.BasisCountingCircleId), result))
            {
                throw new ValidationException($"Duplicate counting circle result provided for the same political business (Business-ID: {result.PoliticalBusinessId} / Counting-Circle-ID: {result.BasisCountingCircleId})");
            }
        }
    }

    public void SetTotalCountOfVoters(Guid politicalBusinessId, string basisCountingCircleId, int totalCountOfVoters)
    {
        if (!_politicalBusinessResults.TryGetValue((politicalBusinessId, basisCountingCircleId), out var result))
        {
            // simply ignore unknown results,
            // usually these are "Testurnen"
            return;
        }

        if (result.TotalCountOfVoters != 0)
        {
            throw new ValidationException("Duplicate count of voters information provided");
        }

        result.TotalCountOfVoters = totalCountOfVoters;
    }

    public void RemoveResults(IEnumerable<(Guid BusinessId, string BasisCountingCircleId)> resultKeyToRemove)
    {
        foreach (var key in resultKeyToRemove)
        {
            _politicalBusinessResults.Remove(key);
        }
    }
}
