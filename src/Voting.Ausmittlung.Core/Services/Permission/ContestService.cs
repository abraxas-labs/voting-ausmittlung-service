// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Permission;

public class ContestService
{
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _politicalBusinessRepo;

    public ContestService(
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> politicalBusinessRepo)
    {
        _contestRepo = contestRepo;
        _politicalBusinessRepo = politicalBusinessRepo;
    }

    public void EnsureInTestingPhase(Contest contest)
    {
        if (contest.State != ContestState.TestingPhase)
        {
            throw new ContestTestingPhaseEndedException();
        }
    }

    public async Task<(Guid ContestId, bool TestingPhaseEnded)> EnsureNotLocked(Guid id)
    {
        var contest = await _contestRepo.Query()
                        .Where(x => x.Id == id)
                        .Select(x => new { State = x.State, TestingPhaseEnded = x.TestingPhaseEnded })
                        .FirstOrDefaultAsync()
                    ?? throw new EntityNotFoundException(id);
        EnsureNotLocked(contest.State);
        return (id, contest.TestingPhaseEnded);
    }

    /// <summary>
    /// Ensures that the contest is not in a "locked" state where not write operations may occur.
    /// </summary>
    /// <param name="contest">The contest to check.</param>
    /// <exception cref="ContestLockedException">Thrown if the contest is in a locked state.</exception>
    public void EnsureNotLocked(Contest contest) => EnsureNotLocked(contest.State);

    /// <summary>
    /// Ensures that the contest of the political business is not in a "locked" state where not write operations may occur.
    /// </summary>
    /// <param name="politicalBusinessId">The political business ID to check.</param>
    /// <exception cref="ContestLockedException">Thrown if the contest is in a locked state.</exception>
    /// <returns>The contest ID of the political business and whether the testing phase of the contest has ended.</returns>
    public async Task<(Guid ContestId, bool TestingPhaseEnded)> EnsureNotLockedByPoliticalBusiness(Guid politicalBusinessId)
    {
        var contest = await _politicalBusinessRepo.Query()
                        .Where(x => x.Id == politicalBusinessId)
                        .Select(x => new { x.Contest.Id, x.Contest.State, x.Contest.TestingPhaseEnded })
                        .FirstOrDefaultAsync()
                    ?? throw new EntityNotFoundException(politicalBusinessId);
        EnsureNotLocked(contest.State);
        return (contest.Id, contest.TestingPhaseEnded);
    }

    private void EnsureNotLocked(ContestState state)
    {
        if (state.IsLocked())
        {
            throw new ContestLockedException();
        }
    }
}
