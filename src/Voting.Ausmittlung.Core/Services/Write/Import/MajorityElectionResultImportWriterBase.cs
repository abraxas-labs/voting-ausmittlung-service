// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
        await ValidateState(electionId, basisCountingCircleId);

        var hasInvalid = mappings.Any(x => x.Target == MajorityElectionWriteInMappingTarget.Invalid);
        var hasIndividual = mappings.Any(m => m.Target == MajorityElectionWriteInMappingTarget.Individual);
        var election = await GetElection(electionId);
        var individualVotesDisabled = GetIndividualVotesDisabled(election);

        if (hasInvalid && !election.Contest.CantonDefaults.MajorityElectionInvalidVotes)
        {
            throw new ValidationException("Invalid votes are not enabled on this election");
        }

        if (hasIndividual && individualVotesDisabled)
        {
            throw new ValidationException("Individual votes are not enabled on this election");
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

    internal async Task ValidateState(Guid electionId, Guid basisCountingCircleId)
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
    }

    internal async IAsyncEnumerable<MajorityElectionResultImport> BuildImports(
        ResultImportMeta importMeta,
        IReadOnlyCollection<VotingImportElectionResult> results)
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

    protected abstract bool GetIndividualVotesDisabled(TElection election);

    private MajorityElectionResultImport ProcessResult(
        VotingImportElectionResult result,
        TElection election,
        IReadOnlyDictionary<Guid, MajorityElectionCandidateBase> candidatesById)
    {
        var importResult = new MajorityElectionResultImport(result.PoliticalBusinessId, Guid.Parse(result.BasisCountingCircleId), result.TotalCountOfVoters);
        importResult.CountOfVoters = result.Ballots.Count;
        var supportsInvalidVotes = election.Contest.CantonDefaults.MajorityElectionInvalidVotes;

        foreach (var ballot in result.Ballots)
        {
            var emptyVoteCount = election.NumberOfMandates - ballot.Positions.Count;
            if (emptyVoteCount < 0)
            {
                throw new ValidationException(
                    $"the number of ballot positions exceeds the number of mandates ({ballot.Positions.Count} vs {election.NumberOfMandates})");
            }

            var processedBallot = ProcessBallot(importResult.PoliticalBusinessId, ballot, candidatesById, emptyVoteCount);
            if (!supportsInvalidVotes)
            {
                processedBallot.EmptyVoteCount += processedBallot.InvalidVoteCount;
                processedBallot.InvalidVoteCount = 0;
            }

            importResult.AddBallot(processedBallot);
        }

        return importResult;
    }

    private MajorityElectionBallot ProcessBallot(
        Guid politicalBussinessId,
        VotingElectionBallot votingBallot,
        IReadOnlyDictionary<Guid, MajorityElectionCandidateBase> candidatesById,
        int emptyVoteCount)
    {
        var ballot = new MajorityElectionBallot(emptyVoteCount);

        foreach (var position in votingBallot.Positions)
        {
            if (position.IsEmpty)
            {
                ballot.EmptyVoteCount++;
                continue;
            }

            if (position.IsWriteIn)
            {
                if (position.WriteInName == null)
                {
                    throw new ValidationException("encountered write in ballot position without a write in name");
                }

                ballot.WriteIns.Add(position.WriteInName);
                continue;
            }

            ValidateCandidateId(politicalBussinessId, candidatesById, position.CandidateId);
            if (!ballot.CandidateIds.Add(position.CandidateId.Value))
            {
                // Duplicate candidate, map to invalid vote
                ballot.InvalidVoteCount++;
            }
        }

        return ballot;
    }

    private void ValidateCandidateId(
        Guid politicalBusinessId,
        IReadOnlyDictionary<Guid, MajorityElectionCandidateBase> candidatesById,
        [NotNull] Guid? candidateId)
    {
        if (candidateId == null)
        {
            throw new ValidationException("encountered non-empty election ballot position without a candidate id");
        }

        if (!candidatesById.TryGetValue(candidateId.Value, out var candidate) ||
            candidate.PoliticalBusinessId != politicalBusinessId)
        {
            throw new EntityNotFoundException(nameof(MajorityElectionCandidate), candidateId);
        }
    }
}
