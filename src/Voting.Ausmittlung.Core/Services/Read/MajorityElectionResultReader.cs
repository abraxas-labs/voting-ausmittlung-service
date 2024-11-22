// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class MajorityElectionResultReader
{
    private readonly IDbRepository<DataContext, MajorityElectionResult> _repo;
    private readonly PermissionService _permissionService;

    public MajorityElectionResultReader(
        IDbRepository<DataContext, MajorityElectionResult> repo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
    }

    public async Task<MajorityElectionResult> Get(Guid electionResultId)
    {
        return await QueryElectionResult(x => x.Id == electionResultId)
               ?? throw new EntityNotFoundException(electionResultId);
    }

    public async Task<MajorityElectionResult> Get(Guid electionId, Guid basisCountingCircleId)
    {
        return await QueryElectionResult(x => x.MajorityElectionId == electionId && x.CountingCircle.BasisCountingCircleId == basisCountingCircleId)
               ?? throw new EntityNotFoundException(new { electionId, basisCountingCircleId });
    }

    public async Task<MajorityElectionResult> GetWithBallotGroups(Guid electionResultId)
    {
        var electionResult = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle)
            .Include(x => x.MajorityElection.Translations)
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.MajorityElection.Contest.Translations)
            .Include(x => x.MajorityElection.Contest.CantonDefaults)
            .Include(x => x.BallotGroupResults).ThenInclude(x => x.BallotGroup.Entries).ThenInclude(x => x.PrimaryMajorityElection!.Translations)
            .Include(x => x.BallotGroupResults).ThenInclude(x => x.BallotGroup.Entries).ThenInclude(x => x.SecondaryMajorityElection!.PrimaryMajorityElection)
            .Include(x => x.BallotGroupResults).ThenInclude(x => x.BallotGroup.Entries).ThenInclude(x => x.SecondaryMajorityElection!.Translations)
            .Include(x => x.BallotGroupResults).ThenInclude(x => x.BallotGroup.Entries).ThenInclude(x => x.Candidates).ThenInclude(x => x.PrimaryElectionCandidate!.Translations)
            .Include(x => x.BallotGroupResults).ThenInclude(x => x.BallotGroup.Entries).ThenInclude(x => x.Candidates).ThenInclude(x => x.SecondaryElectionCandidate!.Translations)
            .FirstOrDefaultAsync(x => x.Id == electionResultId)
            ?? throw new EntityNotFoundException(electionResultId);

        await _permissionService.EnsureCanReadCountingCircle(electionResult.CountingCircleId, electionResult.MajorityElection.ContestId);
        if (electionResult.Entry != MajorityElectionResultEntry.Detailed)
        {
            throw new ValidationException("this is only allowed for detailed result entry");
        }

        electionResult.BallotGroupResults = electionResult.BallotGroupResults
            .Where(x => x.BallotGroup.AllCandidateCountsOk)
            .OrderBy(x => x.BallotGroup.Position)
            .ToList();

        foreach (var ballotGroupResult in electionResult.BallotGroupResults)
        {
            ballotGroupResult.BallotGroup.Entries = ballotGroupResult.BallotGroup.Entries
                .OrderBy(e => e.Election.BusinessType)
                .ThenBy(e => e.Election.Title)
                .ToList();
        }

        return electionResult;
    }

    private async Task<MajorityElectionResult?> QueryElectionResult(
        Expression<Func<MajorityElectionResult, bool>> predicate)
    {
        var result = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle)
            .Include(x => x.MajorityElection.Translations)
            .Include(x => x.MajorityElection.Contest.Translations)
            .Include(x => x.MajorityElection.Contest.CantonDefaults)
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.CandidateResults).ThenInclude(cr => cr.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults).ThenInclude(cr => cr.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.SecondaryMajorityElection.Translations)
            .Include(x => x.BallotGroupResults)
            .Where(predicate)
            .FirstOrDefaultAsync();
        if (result == null)
        {
            return null;
        }

        result.SecondaryMajorityElectionResults = result.SecondaryMajorityElectionResults
            .OrderBy(x => x.SecondaryMajorityElection.PoliticalBusinessNumber)
            .ToList();

        await _permissionService.EnsureCanReadCountingCircle(result.CountingCircleId, result.MajorityElection.ContestId);

        if (result.Entry == MajorityElectionResultEntry.FinalResults && result.State <= CountingCircleResultState.ReadyForCorrection)
        {
            SortCandidateByPosition(result);
        }
        else
        {
            SortCandidateByCount(result);
        }

        return result;
    }

    private void SortCandidateByCount(MajorityElectionResult result)
    {
        result.CandidateResults = result.CandidateResults
            .OrderByDescending(x => x.VoteCount)
            .ThenBy(x => x.CandidatePosition)
            .ToList();
        foreach (var secondaryMajorityElectionResult in result.SecondaryMajorityElectionResults)
        {
            secondaryMajorityElectionResult.CandidateResults = secondaryMajorityElectionResult.CandidateResults
                .OrderByDescending(x => x.VoteCount)
                .ThenBy(x => x.CandidatePosition)
                .ToList();
        }
    }

    private void SortCandidateByPosition(MajorityElectionResult result)
    {
        result.CandidateResults = result.CandidateResults
            .OrderBy(x => x.CandidatePosition)
            .ToList();
        foreach (var secondaryMajorityElectionResult in result.SecondaryMajorityElectionResults)
        {
            secondaryMajorityElectionResult.CandidateResults = secondaryMajorityElectionResult.CandidateResults
                .OrderBy(x => x.CandidatePosition)
                .ToList();
        }
    }
}
