// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ContestResultInitializer
{
    private readonly ResultImportRepo _importRepo;
    private readonly IDbRepository<DataContext, ContestDetails> _contestDetailsRepo;
    private readonly SimpleCountingCircleResultRepo _simpleCountingCircleResultRepo;
    private readonly CountingCircleRepo _countingCircleRepo;
    private readonly ContestCountingCircleDetailsRepo _contestCountingCircleDetailsRepo;
    private readonly VoteRepo _voteRepo;
    private readonly ProportionalElectionRepo _proportionalElectionRepo;
    private readonly MajorityElectionRepo _majorityElectionRepo;
    private readonly VoteResultBuilder _voteResultBuilder;
    private readonly VoteEndResultInitializer _voteEndResultInitializer;
    private readonly MajorityElectionResultBuilder _majorityElectionResultBuilder;
    private readonly MajorityElectionEndResultInitializer _majorityElectionEndResultInitializer;
    private readonly ProportionalElectionResultBuilder _proportionalElectionResultBuilder;
    private readonly ProportionalElectionEndResultInitializer _proportionalElectionEndResultInitializer;
    private readonly IDbRepository<DataContext, ContestDomainOfInfluenceDetails> _contestDomainOfInfluenceDetailsRepo;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepo;
    private readonly ProportionalElectionUnionEndResultInitializer _proportionalElectionUnionEndResultInitializer;
    private readonly ProportionalElectionUnionRepo _proportionalElectionUnionRepo;
    private readonly DoubleProportionalResultBuilder _proportionalElectionDpResultBuilder;

    public ContestResultInitializer(
        ResultImportRepo importRepo,
        IDbRepository<DataContext, ContestDetails> contestDetailsRepo,
        SimpleCountingCircleResultRepo simpleCountingCircleResultRepo,
        CountingCircleRepo countingCircleRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        VoteRepo voteRepo,
        ProportionalElectionRepo proportionalElectionRepo,
        MajorityElectionRepo majorityElectionRepo,
        VoteResultBuilder voteResultBuilder,
        VoteEndResultInitializer voteEndResultInitializer,
        MajorityElectionResultBuilder majorityElectionResultBuilder,
        MajorityElectionEndResultInitializer majorityElectionEndResultInitializer,
        ProportionalElectionResultBuilder proportionalElectionResultBuilder,
        ProportionalElectionEndResultInitializer proportionalElectionEndResultInitializer,
        IDbRepository<DataContext, ContestDomainOfInfluenceDetails> contestDomainOfInfluenceDetailsRepo,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        ProportionalElectionUnionEndResultInitializer proportionalElectionUnionEndResultInitializer,
        ProportionalElectionUnionRepo proportionalElectionUnionRepo,
        DoubleProportionalResultBuilder proportionalElectionDpResultBuilder)
    {
        _importRepo = importRepo;
        _contestDetailsRepo = contestDetailsRepo;
        _simpleCountingCircleResultRepo = simpleCountingCircleResultRepo;
        _countingCircleRepo = countingCircleRepo;
        _contestCountingCircleDetailsRepo = contestCountingCircleDetailsRepo;
        _voteRepo = voteRepo;
        _proportionalElectionRepo = proportionalElectionRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _voteResultBuilder = voteResultBuilder;
        _voteEndResultInitializer = voteEndResultInitializer;
        _majorityElectionResultBuilder = majorityElectionResultBuilder;
        _majorityElectionEndResultInitializer = majorityElectionEndResultInitializer;
        _proportionalElectionResultBuilder = proportionalElectionResultBuilder;
        _proportionalElectionEndResultInitializer = proportionalElectionEndResultInitializer;
        _contestDomainOfInfluenceDetailsRepo = contestDomainOfInfluenceDetailsRepo;
        _protocolExportRepo = protocolExportRepo;
        _proportionalElectionUnionEndResultInitializer = proportionalElectionUnionEndResultInitializer;
        _proportionalElectionUnionRepo = proportionalElectionUnionRepo;
        _proportionalElectionDpResultBuilder = proportionalElectionDpResultBuilder;
    }

    internal async Task ResetContestResults(Guid contestId, Guid? contestDetailsId)
    {
        if (contestDetailsId.HasValue)
        {
            await _contestDetailsRepo.DeleteByKey(contestDetailsId.Value);
        }

        await DeleteContestDomainOfInfluenceDetails(contestId);

        await ResetContestCountingCircleDetails(contestId);
        await _countingCircleRepo.SetMustUpdateContactPerson(contestId);

        await _importRepo.DeleteOfContest(contestId);
        await ResetVoteResults(contestId);
        await ResetProportionalElectionResults(contestId);
        await ResetMajorityElectionResults(contestId);
        await ResetProportionalElectionUnionResults(contestId);
        await _simpleCountingCircleResultRepo.Reset(contestId);

        await DeleteProtocolExports(contestId);
    }

    private async Task ResetContestCountingCircleDetails(Guid contestId)
    {
        var details = await _contestCountingCircleDetailsRepo.Query()
            .Where(x => x.ContestId == contestId)
            .Include(x => x.CountingCircle)
            .ToListAsync();

        await _contestCountingCircleDetailsRepo.DeleteRangeByKey(details.Select(x => x.Id));
        await _contestCountingCircleDetailsRepo.CreateRange(details.Select(x => new ContestCountingCircleDetails
        {
            Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, x.CountingCircle.BasisCountingCircleId, true),
            ContestId = contestId,
            CountingCircleId = x.CountingCircleId,
            EVoting = x.EVoting,
        }));
    }

    private async Task ResetVoteResults(Guid contestId)
    {
        var votes = await _voteRepo.Query()
            .Where(v => v.ContestId == contestId)
            .Select(v => new { v.Id, v.DomainOfInfluenceId })
            .ToListAsync();

        foreach (var vote in votes)
        {
            await _voteResultBuilder.ResetForVote(vote.Id, vote.DomainOfInfluenceId);
            await _voteEndResultInitializer.ResetForVote(vote.Id);
        }
    }

    private async Task ResetProportionalElectionResults(Guid contestId)
    {
        var elections = await _proportionalElectionRepo.Query()
            .Where(e => e.ContestId == contestId)
            .Select(e => new { e.Id, e.DomainOfInfluenceId })
            .ToListAsync();

        foreach (var election in elections)
        {
            await _proportionalElectionResultBuilder.ResetForElection(election.Id, election.DomainOfInfluenceId);
            await _proportionalElectionEndResultInitializer.ResetForElection(election.Id);
        }
    }

    private async Task ResetMajorityElectionResults(Guid contestId)
    {
        var elections = await _majorityElectionRepo.Query()
            .Where(e => e.ContestId == contestId)
            .Select(e => new { e.Id, e.DomainOfInfluenceId })
            .ToListAsync();

        foreach (var election in elections)
        {
            await _majorityElectionResultBuilder.ResetForElection(election.Id, election.DomainOfInfluenceId);
            await _majorityElectionEndResultInitializer.ResetForElection(election.Id);
        }
    }

    private async Task ResetProportionalElectionUnionResults(Guid contestId)
    {
        var unionIds = await _proportionalElectionUnionRepo.Query()
            .Where(e => e.ContestId == contestId)
            .Select(e => e.Id)
            .ToListAsync();

        foreach (var unionId in unionIds)
        {
            await _proportionalElectionUnionEndResultInitializer.ResetForUnion(unionId);
        }

        await _proportionalElectionDpResultBuilder.ResetForContest(contestId);
    }

    private async Task DeleteContestDomainOfInfluenceDetails(Guid contestId)
    {
        var idsToDelete = await _contestDomainOfInfluenceDetailsRepo.Query()
            .Where(x => x.ContestId == contestId)
            .Select(x => x.Id)
            .ToListAsync();

        await _contestDomainOfInfluenceDetailsRepo.DeleteRangeByKey(idsToDelete);
    }

    private async Task DeleteProtocolExports(Guid contestId)
    {
        var idsToDelete = await _protocolExportRepo.Query()
            .Where(x => x.ContestId == contestId)
            .Select(x => x.Id)
            .ToListAsync();

        await _protocolExportRepo.DeleteRangeByKey(idsToDelete);
    }
}
