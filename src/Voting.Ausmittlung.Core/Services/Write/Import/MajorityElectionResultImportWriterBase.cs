// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public abstract class MajorityElectionResultImportWriterBase<TElection>
    : PoliticalBusinessResultImportWriter<MajorityElectionResultAggregate, MajorityElectionResult>
    where TElection : Election
{
    protected MajorityElectionResultImportWriterBase(IAggregateRepository aggregateRepository)
        : base(aggregateRepository)
    {
    }

    internal async Task ValidateWriteIns(
        Guid electionId,
        Guid basisCountingCircleId,
        IReadOnlyCollection<MajorityElectionWriteIn> mappings)
    {
        var resultId = await GetPrimaryResultId(electionId, basisCountingCircleId);
        var aggregate = await GetAggregate(resultId);

        if (aggregate is
            {
                State: not (
                CountingCircleResultState.Initial
                or CountingCircleResultState.ReadyForCorrection
                or CountingCircleResultState.SubmissionOngoing)
            })
        {
            throw new ValidationException("WriteIns are only possible if the result is in a mutable state");
        }

        var hasInvalid = mappings.Any(x => x.Target == MajorityElectionWriteInMappingTarget.Invalid);
        var election = await GetElection(electionId);

        // In elections with a single mandate the mapping target "invalid" is valid, since a whole ballot can be invalid.
        if (hasInvalid && election.NumberOfMandates > 1 && !election.DomainOfInfluence.CantonDefaults.MajorityElectionInvalidVotes)
        {
            throw new ValidationException("Invalid votes are not enabled on this election");
        }

        var candidateIds = mappings
            .Where(x => x.Target == MajorityElectionWriteInMappingTarget.Candidate)
            .Select(x => x.CandidateId ?? Guid.Empty)
            .ToHashSet();

        var availableCandidateIds = await GetCandidateIds(electionId);
        candidateIds.ExceptWith(availableCandidateIds);
        if (candidateIds.Count > 0)
        {
            throw new ValidationException("Invalid candidates provided");
        }
    }

    internal async IAsyncEnumerable<MajorityElectionResultImport> BuildImports(
        ResultImportMeta importMeta,
        IReadOnlyCollection<EVotingElectionResult> results)
    {
        var electionIds = results.Select(x => x.PoliticalBusinessId).ToHashSet();
        var elections = await LoadElections(importMeta.ContestId, electionIds);

        var electionsById = elections.ToDictionary(x => x.Id);
        var candidatesById = elections.SelectMany(GetCandidates).ToDictionary(x => x.Id);

        foreach (var result in results)
        {
            if (!electionsById.TryGetValue(result.PoliticalBusinessId, out var election))
            {
                throw new EntityNotFoundException(nameof(MajorityElection), result.PoliticalBusinessId);
            }

            yield return ProcessResult(result, election, candidatesById);
        }
    }

    protected abstract Task<Guid> GetPrimaryResultId(Guid electionId, Guid basisCountingCircleId);

    protected abstract Task<List<TElection>> LoadElections(Guid contestId, IReadOnlyCollection<Guid> electionIds);

    protected abstract Task<TElection> GetElection(Guid electionId);

    protected abstract Task<List<Guid>> GetCandidateIds(Guid electionId);

    protected abstract IEnumerable<MajorityElectionCandidateBase> GetCandidates(TElection election);

    private MajorityElectionResultImport ProcessResult(
        EVotingElectionResult result,
        TElection election,
        IReadOnlyDictionary<Guid, MajorityElectionCandidateBase> candidatesById)
    {
        var supportsInvalidVotes = election.DomainOfInfluence.CantonDefaults.MajorityElectionInvalidVotes;
        var importResult = new MajorityElectionResultImport(result.PoliticalBusinessId, result.BasisCountingCircleId);
        importResult.CountOfVoters = result.Ballots.Count;
        foreach (var ballot in result.Ballots)
        {
            if (election.NumberOfMandates < ballot.Positions.Count)
            {
                throw new ValidationException(
                    $"the number of ballot positions exceeds the number of mandates ({ballot.Positions.Count} vs {election.NumberOfMandates})");
            }

            if (election.NumberOfMandates == 1 && (ballot.Positions.Count != 1 || ballot.Positions.First().IsEmpty))
            {
                throw new ValidationException("empty position provided with single mandate");
            }

            var candidatesOnThisBallot = new HashSet<Guid>();
            var writeInsOnThisBallot = new HashSet<string>(MajorityElectionResultImport.WriteInComparer);
            foreach (var position in ballot.Positions)
            {
                ProcessPosition(
                    supportsInvalidVotes,
                    position,
                    importResult,
                    candidatesById,
                    candidatesOnThisBallot,
                    writeInsOnThisBallot);
            }

            if (election.NumberOfMandates > ballot.Positions.Count)
            {
                importResult.EmptyVoteCount += election.NumberOfMandates - ballot.Positions.Count;
            }
        }

        return importResult;
    }

    private void ProcessPosition(
        bool supportsInvalidVotes,
        EVotingElectionBallotPosition position,
        MajorityElectionResultImport importResult,
        IReadOnlyDictionary<Guid, MajorityElectionCandidateBase> candidatesById,
        ISet<Guid> candidatesOnThisBallot,
        ISet<string> writeInsOnThisBallot)
    {
        if (position.IsEmpty)
        {
            importResult.EmptyVoteCount++;
            return;
        }

        if (position.IsWriteIn)
        {
            HandleWriteIn(supportsInvalidVotes, importResult, writeInsOnThisBallot, position.WriteInName);
            return;
        }

        HandleCandidatePosition(supportsInvalidVotes, importResult, candidatesOnThisBallot, candidatesById, position.CandidateId);
    }

    private void HandleCandidatePosition(
        bool supportsInvalidVotes,
        MajorityElectionResultImport importResult,
        ISet<Guid> candidatesOnThisBallot,
        IReadOnlyDictionary<Guid, MajorityElectionCandidateBase> candidatesById,
        Guid? candidateId)
    {
        if (candidateId == null)
        {
            throw new ValidationException("encountered non-empty election ballot position without a candidate id");
        }

        if (!candidatesById.TryGetValue(candidateId.Value, out var candidate) ||
            candidate.PoliticalBusinessId != importResult.PoliticalBusinessId)
        {
            throw new EntityNotFoundException(nameof(MajorityElectionCandidate), candidateId);
        }

        // try to add candidate
        // if already added it is a duplicate
        // duplicates count towards invalid vote counts
        // or empty (if invalid vote counts are disabled for this election)
        if (candidatesOnThisBallot.Add(candidateId.Value))
        {
            importResult.AddCandidateVote(candidateId.Value);
            return;
        }

        importResult.AddInvalidOrEmptyVote(supportsInvalidVotes);
    }

    private void HandleWriteIn(
        bool supportsInvalidVotes,
        MajorityElectionResultImport importResult,
        ISet<string> writeInsOnThisBallot,
        string? writeInName)
    {
        if (writeInName == null)
        {
            throw new ValidationException("encountered write in ballot position without a write in name");
        }

        // if write in is a duplicate,
        // duplicates count towards invalid vote counts
        // or empty (if invalid vote counts are disabled for this election)
        if (writeInsOnThisBallot.Add(writeInName))
        {
            importResult.AddMissingWriteIn(writeInName);
            return;
        }

        importResult.AddInvalidOrEmptyVote(supportsInvalidVotes);
    }
}
