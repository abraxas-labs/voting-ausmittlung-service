// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ResultWriter
{
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IDbRepository<DataContext, Vote> _voteRepository;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepository;
    private readonly PermissionService _permissionService;

    public ResultWriter(
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        PermissionService permissionService,
        IDbRepository<DataContext, Vote> voteRepository,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepository)
    {
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _permissionService = permissionService;
        _voteRepository = voteRepository;
        _majorityElectionRepository = majorityElectionRepository;
    }

    public async Task StartSubmission(ResultList data)
    {
        // Only start the submission for the correct states and users
        if (data.Contest.State.IsLocked()
            || !_permissionService.IsErfassungElectionAdmin()
            || !data.CountingCircle.ResponsibleAuthority.SecureConnectId.Equals(_permissionService.TenantId, StringComparison.Ordinal))
        {
            return;
        }

        var results = data.Results
            .Where(r => r.State == CountingCircleResultState.Initial)
            .GroupBy(x => x.PoliticalBusiness!.BusinessType);

        foreach (var groupedResults in results)
        {
            switch (groupedResults.Key)
            {
                case PoliticalBusinessType.Vote:
                    await StartVoteSubmission(data.CountingCircle, groupedResults.ToList(), data.Contest.TestingPhaseEnded);
                    break;
                case PoliticalBusinessType.ProportionalElection:
                    await StartProportionalElectionSubmission(data.CountingCircle, groupedResults, data.Contest.TestingPhaseEnded);
                    break;
                case PoliticalBusinessType.MajorityElection:
                    await StartMajorityElectionSubmission(data.CountingCircle, groupedResults.ToList(), data.Contest.TestingPhaseEnded);
                    break;
            }
        }
    }

    private async Task StartVoteSubmission(CountingCircle countingCircle, IReadOnlyCollection<SimpleCountingCircleResult> results, bool testingPhaseEnded)
    {
        var ids = results.Select(x => x.PoliticalBusinessId).ToList();

        // if the result entry is enforced and only final results should be provided, it should be defined automatically
        var idsWithEnforcedFinalResultEntryList = await _voteRepository.Query()
            .Where(x => x.EnforceResultEntryForCountingCircles && x.ResultEntry == VoteResultEntry.FinalResults && ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();
        var idsWithEnforcedFinalResultEntry = idsWithEnforcedFinalResultEntryList.ToHashSet();

        foreach (var result in results)
        {
            result.State = CountingCircleResultState.SubmissionOngoing;

            var aggregate = _aggregateFactory.New<VoteResultAggregate>();
            aggregate.StartSubmission(countingCircle.BasisCountingCircleId, result.PoliticalBusinessId, result.PoliticalBusiness!.ContestId, testingPhaseEnded);

            if (idsWithEnforcedFinalResultEntry.Contains(result.PoliticalBusinessId))
            {
                aggregate.DefineEntry(VoteResultEntry.FinalResults, result.PoliticalBusiness.ContestId);
            }

            await _aggregateRepository.Save(aggregate);
        }
    }

    private async Task StartProportionalElectionSubmission(CountingCircle countingCircle, IEnumerable<SimpleCountingCircleResult> results, bool testingPhaseEnded)
    {
        foreach (var result in results)
        {
            result.State = CountingCircleResultState.SubmissionOngoing;

            var aggregate = _aggregateFactory.New<ProportionalElectionResultAggregate>();
            aggregate.StartSubmission(countingCircle.BasisCountingCircleId, result.PoliticalBusinessId, result.PoliticalBusiness!.ContestId, testingPhaseEnded);
            await _aggregateRepository.Save(aggregate);
        }
    }

    private async Task StartMajorityElectionSubmission(CountingCircle countingCircle, IReadOnlyCollection<SimpleCountingCircleResult> results, bool testingPhaseEnded)
    {
        var ids = results.Select(x => x.PoliticalBusinessId).ToList();

        // if the result entry is enforced and only final results should be provided, it should be defined automatically
        var idsWithEnforcedFinalResultEntryList = await _majorityElectionRepository.Query()
            .Where(x => x.EnforceResultEntryForCountingCircles && x.ResultEntry == MajorityElectionResultEntry.FinalResults && ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();
        var idsWithEnforcedFinalResultEntry = idsWithEnforcedFinalResultEntryList.ToHashSet();

        foreach (var result in results)
        {
            result.State = CountingCircleResultState.SubmissionOngoing;

            var aggregate = _aggregateFactory.New<MajorityElectionResultAggregate>();
            aggregate.StartSubmission(countingCircle.BasisCountingCircleId, result.PoliticalBusinessId, result.PoliticalBusiness!.ContestId, testingPhaseEnded);

            // if the result entry is enforced and only final results should be provided, it should be defined automatically
            if (idsWithEnforcedFinalResultEntry.Contains(result.PoliticalBusinessId))
            {
                aggregate.DefineEntry(MajorityElectionResultEntry.FinalResults, result.PoliticalBusiness.ContestId);
            }

            await _aggregateRepository.Save(aggregate);
        }
    }
}
