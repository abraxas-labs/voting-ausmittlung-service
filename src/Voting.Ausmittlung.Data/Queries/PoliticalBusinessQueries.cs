// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Queries;

public class PoliticalBusinessQueries
{
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly IDbRepository<DataContext, ProportionalElection> _proportionalElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;

    private readonly VoteResultRepo _voteResultRepo;
    private readonly ProportionalElectionResultRepo _proportionalElectionResultRepo;
    private readonly MajorityElectionResultRepo _majorityElectionResultRepo;

    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _proportionalElectionUnionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionUnion> _majorityElectionUnionRepo;

    public PoliticalBusinessQueries(
        IDbRepository<DataContext, Vote> voteRepo,
        IDbRepository<DataContext, ProportionalElection> proportionalElectionRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        VoteResultRepo voteResultRepo,
        ProportionalElectionResultRepo proportionalElectionResultRepo,
        MajorityElectionResultRepo majorityElectionResultRepo,
        IDbRepository<DataContext, ProportionalElectionUnion> proportionalElectionUnionRepo,
        IDbRepository<DataContext, MajorityElectionUnion> majorityElectionUnionRepo)
    {
        _voteRepo = voteRepo;
        _proportionalElectionRepo = proportionalElectionRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _voteResultRepo = voteResultRepo;
        _proportionalElectionResultRepo = proportionalElectionResultRepo;
        _majorityElectionResultRepo = majorityElectionResultRepo;
        _proportionalElectionUnionRepo = proportionalElectionUnionRepo;
        _majorityElectionUnionRepo = majorityElectionUnionRepo;
    }

    public IQueryable<PoliticalBusiness> PoliticalBusinessQuery(PoliticalBusinessType type)
    {
        return type switch
        {
            PoliticalBusinessType.Vote => _voteRepo.Query(),
            PoliticalBusinessType.ProportionalElection => _proportionalElectionRepo.Query(),
            PoliticalBusinessType.MajorityElection => _majorityElectionRepo.Query(),
            _ => throw new InvalidOperationException($"invalid political business type {type}"),
        };
    }

    public IQueryable<CountingCircleResult> CountingCircleResultQueryIncludingPoliticalBusiness(
        PoliticalBusinessType type,
        Guid? politicalBusinessId = null,
        Guid? basisCountingCircleId = null)
    {
        return type switch
        {
            PoliticalBusinessType.Vote => _voteResultRepo
                .Query()
                .Include(v => v.Vote.DomainOfInfluence)
                .Where(v => (!politicalBusinessId.HasValue || v.VoteId == politicalBusinessId.Value)
                        && (!basisCountingCircleId.HasValue || v.CountingCircle.BasisCountingCircleId == basisCountingCircleId.Value)),
            PoliticalBusinessType.ProportionalElection => _proportionalElectionResultRepo
                .Query()
                .Include(p => p.ProportionalElection.DomainOfInfluence)
                .Where(p => (!politicalBusinessId.HasValue || p.ProportionalElectionId == politicalBusinessId)
                    && (!basisCountingCircleId.HasValue || p.CountingCircle.BasisCountingCircleId == basisCountingCircleId.Value)),
            PoliticalBusinessType.MajorityElection => _majorityElectionResultRepo
                .Query()
                .Include(m => m.MajorityElection.DomainOfInfluence)
                .Where(m => (!politicalBusinessId.HasValue || m.MajorityElectionId == politicalBusinessId)
                    && (!basisCountingCircleId.HasValue || m.CountingCircle.BasisCountingCircleId == basisCountingCircleId.Value)),
            _ => throw new InvalidOperationException($"invalid political business type {type}"),
        };
    }

    public IQueryable<PoliticalBusinessUnion> PoliticalBusinessUnionQueryIncludingPoliticalBusinesses(
        PoliticalBusinessType type)
    {
        return type switch
        {
            PoliticalBusinessType.ProportionalElection => _proportionalElectionUnionRepo
                .Query()
                .Include(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElection),
            PoliticalBusinessType.MajorityElection => _majorityElectionUnionRepo
                .Query()
                .Include(x => x.MajorityElectionUnionEntries)
                .ThenInclude(x => x.MajorityElection),
            _ => throw new InvalidOperationException($"invalid political business type {type}"),
        };
    }
}
