// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public class MajorityElectionResultImportWriter : MajorityElectionResultImportWriterBase<MajorityElection>
{
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _majorityElectionResult;
    private readonly IDbRepository<DataContext, MajorityElectionCandidate> _candidateRepo;

    public MajorityElectionResultImportWriter(
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionCandidate> candidateRepo,
        IDbRepository<DataContext, MajorityElectionResult> majorityElectionResult)
        : base(aggregateRepository)
    {
        _majorityElectionRepo = majorityElectionRepo;
        _candidateRepo = candidateRepo;
        _majorityElectionResult = majorityElectionResult;
    }

    protected override async Task<Guid> GetPrimaryResultId(Guid electionId, Guid basisCountingCircleId)
    {
        return await _majorityElectionResult
                   .Query()
                   .Where(x => x.MajorityElectionId == electionId && x.CountingCircle.BasisCountingCircleId == basisCountingCircleId)
                   .Select(x => (Guid?)x.Id)
                   .FirstOrDefaultAsync()
               ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { electionId, basisCountingCircleId });
    }

    protected override Task<List<MajorityElection>> LoadElections(Guid contestId, IReadOnlyCollection<Guid> electionIds)
    {
        return _majorityElectionRepo
            .Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == contestId && electionIds.Contains(x.Id))
            .Include(x => x.MajorityElectionCandidates)
            .Include(x => x.Translations)
            .Include(x => x.ElectionGroup)
            .ToListAsync();
    }

    protected override Task<List<Guid>> GetCandidateIds(Guid electionId)
    {
        return _candidateRepo.Query()
            .Where(x => x.MajorityElectionId == electionId)
            .Select(x => x.Id)
            .ToListAsync();
    }

    protected override IEnumerable<MajorityElectionCandidateBase> GetCandidates(MajorityElection election)
        => election.MajorityElectionCandidates;

    protected override Task<bool> SupportsInvalidVotes(Guid electionId)
        => _majorityElectionRepo.Query().AnyAsync(x => x.Id == electionId && x.InvalidVotes);

    protected override bool SupportsInvalidVotes(MajorityElection election)
        => election.InvalidVotes;

    protected override IQueryable<MajorityElectionResult> BuildResultsQuery(Guid contestId)
        => _majorityElectionRepo.Query()
            .Where(x => x.ContestId == contestId)
            .SelectMany(x => x.Results);
}
