// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class CountingCircleResultsInitializer
{
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly IDbRepository<DataContext, ProportionalElection> _proportionalElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;
    private readonly VoteResultBuilder _voteResultBuilder;
    private readonly ProportionalElectionResultBuilder _proportionalElectionResultBuilder;
    private readonly MajorityElectionResultBuilder _majorityElectionResultBuilder;
    private readonly SimpleCountingCircleResultRepo _ccResultRepo;

    public CountingCircleResultsInitializer(
        IDbRepository<DataContext, Vote> voteRepo,
        IDbRepository<DataContext, ProportionalElection> proportionalElectionRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        VoteResultBuilder voteResultBuilder,
        ProportionalElectionResultBuilder proportionalElectionResultBuilder,
        MajorityElectionResultBuilder majorityElectionResultBuilder,
        SimpleCountingCircleResultRepo ccResultRepo)
    {
        _voteRepo = voteRepo;
        _proportionalElectionRepo = proportionalElectionRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _voteResultBuilder = voteResultBuilder;
        _proportionalElectionResultBuilder = proportionalElectionResultBuilder;
        _majorityElectionResultBuilder = majorityElectionResultBuilder;
        _ccResultRepo = ccResultRepo;
    }

    public async Task InitializeResults(IReadOnlyCollection<Guid> domainOfInfluenceIds)
    {
        await InitializeVoteResults(domainOfInfluenceIds);
        await InitializeProportionalElectionResults(domainOfInfluenceIds);
        await InitializeMajorityElectionResults(domainOfInfluenceIds);
    }

    private async Task InitializeVoteResults(IEnumerable<Guid> domainOfInfluenceIds)
    {
        var votesToUpdate = await _voteRepo.Query()
            .Where(v => domainOfInfluenceIds.Contains(v.DomainOfInfluenceId))
            .Select(v => new { v.Id, v.DomainOfInfluenceId, v.ContestId })
            .ToListAsync();
        foreach (var vote in votesToUpdate)
        {
            await _voteResultBuilder.RebuildForVote(vote.Id, vote.DomainOfInfluenceId, false, vote.ContestId);
            await _ccResultRepo.Sync(vote.Id, vote.DomainOfInfluenceId, false);
        }
    }

    private async Task InitializeProportionalElectionResults(IEnumerable<Guid> domainOfInfluenceIds)
    {
        var electionsToUpdate = await _proportionalElectionRepo.Query()
            .Where(v => domainOfInfluenceIds.Contains(v.DomainOfInfluenceId))
            .Select(v => new { v.Id, v.DomainOfInfluenceId, v.ContestId })
            .ToListAsync();
        foreach (var election in electionsToUpdate)
        {
            await _proportionalElectionResultBuilder.RebuildForElection(election.Id, election.DomainOfInfluenceId, false, election.ContestId);
            await _ccResultRepo.Sync(election.Id, election.DomainOfInfluenceId, false);
        }
    }

    private async Task InitializeMajorityElectionResults(IEnumerable<Guid> domainOfInfluenceIds)
    {
        var electionsToUpdate = await _majorityElectionRepo.Query()
            .Where(v => domainOfInfluenceIds.Contains(v.DomainOfInfluenceId))
            .Select(v => new { v.Id, v.DomainOfInfluenceId, v.ContestId })
            .ToListAsync();
        foreach (var election in electionsToUpdate)
        {
            await _majorityElectionResultBuilder.RebuildForElection(election.Id, election.DomainOfInfluenceId, false, election.ContestId);
            await _ccResultRepo.Sync(election.Id, election.DomainOfInfluenceId, false);
        }
    }
}
