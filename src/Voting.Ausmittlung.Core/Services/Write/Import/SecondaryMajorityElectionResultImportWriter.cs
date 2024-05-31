// (c) Copyright 2024 by Abraxas Informatik AG
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

public class SecondaryMajorityElectionResultImportWriter : MajorityElectionResultImportWriterBase<SecondaryMajorityElection>
{
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _electionRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionCandidate> _candidateRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResult> _resultRepo;

    public SecondaryMajorityElectionResultImportWriter(
        IDbRepository<DataContext, SecondaryMajorityElection> electionRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionCandidate> candidateRepo,
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, SecondaryMajorityElectionResult> resultRepo)
        : base(aggregateRepository)
    {
        _electionRepo = electionRepo;
        _candidateRepo = candidateRepo;
        _resultRepo = resultRepo;
    }

    protected override async Task<Guid> GetPrimaryResultId(Guid electionId, Guid basisCountingCircleId)
    {
        return await _resultRepo
                   .Query()
                   .Where(x => x.SecondaryMajorityElectionId == electionId && x.PrimaryResult.CountingCircle.BasisCountingCircleId == basisCountingCircleId)
                   .Select(x => (Guid?)x.PrimaryResultId)
                   .FirstOrDefaultAsync()
               ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { electionId, basisCountingCircleId });
    }

    protected override Task<List<SecondaryMajorityElection>> LoadElections(Guid contestId, IReadOnlyCollection<Guid> electionIds)
    {
        return _electionRepo
            .Query()
            .AsSplitQuery()
            .Where(x => x.PrimaryMajorityElection.ContestId == contestId && electionIds.Contains(x.Id))
            .Include(x => x.Candidates)
            .Include(x => x.Translations)
            .Include(x => x.PrimaryMajorityElection)
            .Include(x => x.PrimaryMajorityElection.Contest.CantonDefaults)
            .ToListAsync();
    }

    protected override async Task<SecondaryMajorityElection> GetElection(Guid electionId)
    {
        return await _electionRepo.Query()
                   .AsSplitQuery()
                   .Include(x => x.PrimaryMajorityElection.Contest.CantonDefaults)
                   .FirstOrDefaultAsync(x => x.Id == electionId)
               ?? throw new EntityNotFoundException(electionId);
    }

    protected override Task<List<Guid>> GetCandidateIds(Guid electionId)
    {
        return _candidateRepo.Query()
            .Where(x => x.SecondaryMajorityElectionId == electionId)
            .Select(x => x.Id)
            .ToListAsync();
    }

    protected override IEnumerable<MajorityElectionCandidateBase> GetCandidates(SecondaryMajorityElection election)
        => election.Candidates;

    protected override IQueryable<MajorityElectionResult> BuildResultsQuery(Guid contestId)
        => _electionRepo.Query()
            .Where(x => x.ContestId == contestId)
            .SelectMany(x => x.PrimaryMajorityElection.Results);
}
