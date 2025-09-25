// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class CountingCircleResultBuilder
{
    private readonly VoteResultRepo _voteResultRepo;
    private readonly ProportionalElectionResultRepo _proportionalElectionResultRepo;
    private readonly MajorityElectionResultRepo _majorityElectionResultRepo;
    private readonly IDbRepository<DataContext, BallotResult> _ballotResultRepo;

    public CountingCircleResultBuilder(
        VoteResultRepo voteResultRepo,
        ProportionalElectionResultRepo proportionalElectionResultRepo,
        MajorityElectionResultRepo majorityElectionResultRepo,
        IDbRepository<DataContext, BallotResult> ballotResultRepo)
    {
        _voteResultRepo = voteResultRepo;
        _proportionalElectionResultRepo = proportionalElectionResultRepo;
        _majorityElectionResultRepo = majorityElectionResultRepo;
        _ballotResultRepo = ballotResultRepo;
    }

    public async Task UpdateCountOfVotersForCountingCircleResults(IReadOnlyCollection<ContestCountingCircleDetails> detailsList, bool skipResultStateCheck)
    {
        foreach (var details in detailsList)
        {
            await UpdateCountOfVotersForVoteResults(details, skipResultStateCheck);
            await UpdateCountOfVotersForProportionalElectionResults(details, skipResultStateCheck);
            await UpdateCountOfVotersForMajorityElectionResults(details, skipResultStateCheck);
        }
    }

    public async Task UpdateCountOfVotersForCountingCircleResults(ContestCountingCircleDetails details, bool skipResultStateCheck)
    {
        await UpdateCountOfVotersForVoteResults(details, skipResultStateCheck);
        await UpdateCountOfVotersForProportionalElectionResults(details, skipResultStateCheck);
        await UpdateCountOfVotersForMajorityElectionResults(details, skipResultStateCheck);
    }

    private async Task UpdateCountOfVotersForVoteResults(ContestCountingCircleDetails details, bool skipResultStateCheck)
    {
        var ballotResults = new List<BallotResult>();

        var voteResults = await _voteResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.DomainOfInfluence)
            .Include(x => x.Results).ThenInclude(br => br.CountOfVoters)
            .Where(vr => vr.CountingCircleId == details.CountingCircleId)
            .ToListAsync();

        foreach (var voteResult in voteResults)
        {
            UpdateCountOfVoters(voteResult, details, skipResultStateCheck);
            foreach (var ballotResult in voteResult.Results)
            {
                UpdateVoterParticipation(ballotResult.CountOfVoters, voteResult.Vote.DomainOfInfluence, details);
                ballotResults.Add(ballotResult);
                ballotResult.VoteResult = null!;
            }

            // ensures that entities are not tracked multiple times.
            voteResult.Vote = null!;
            voteResult.Results = null!;
        }

        await _voteResultRepo.UpdateRange(voteResults);
        await _ballotResultRepo.UpdateRange(ballotResults);
    }

    private async Task UpdateCountOfVotersForProportionalElectionResults(ContestCountingCircleDetails details, bool skipResultStateCheck)
    {
        var results = await _proportionalElectionResultRepo.Query()
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.CountOfVoters)
            .Where(vr => vr.CountingCircleId == details.CountingCircleId)
            .ToListAsync();

        foreach (var result in results)
        {
            UpdateCountOfVoters(result, details, skipResultStateCheck);
            UpdateVoterParticipation(result.CountOfVoters, result.ProportionalElection.DomainOfInfluence, details);

            // ensures that entities are not tracked multiple times.
            result.ProportionalElection = null!;
        }

        await _proportionalElectionResultRepo.UpdateRange(results);
    }

    private async Task UpdateCountOfVotersForMajorityElectionResults(ContestCountingCircleDetails details, bool skipResultStateCheck)
    {
        var results = await _majorityElectionResultRepo.Query()
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.CountOfVoters)
            .Where(vr => vr.CountingCircleId == details.CountingCircleId)
            .ToListAsync();

        foreach (var result in results)
        {
            UpdateCountOfVoters(result, details, skipResultStateCheck);
            UpdateVoterParticipation(result.CountOfVoters, result.MajorityElection.DomainOfInfluence, details);

            // ensures that entities are not tracked multiple times.
            result.MajorityElection = null!;
        }

        await _majorityElectionResultRepo.UpdateRange(results);
    }

    private void UpdateCountOfVoters(CountingCircleResult result, ContestCountingCircleDetails details, bool skipResultStateCheck)
    {
        if (result.State.IsSubmissionDone() && !skipResultStateCheck)
        {
            // There may be rare race conditions, where an election admin updates the count of voters
            // while another finishes the submission of a political business.
            throw new ContestCountingCircleDetailsNotUpdatableException();
        }

        result.TotalCountOfVoters = details.GetTotalCountOfVotersForDomainOfInfluence(result.PoliticalBusiness.DomainOfInfluence);
    }

    private void UpdateVoterParticipation(PoliticalBusinessNullableCountOfVoters countOfVoters, DomainOfInfluence doi, ContestCountingCircleDetails details)
        => countOfVoters.UpdateVoterParticipation(details.GetTotalCountOfVotersForDomainOfInfluence(doi));
}
