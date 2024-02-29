// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;

public class WabstiCContestDetailsAttacher
{
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _contestDetailsRepo;
    private readonly CountOfVotersInformationSubTotalRepo _countOfVotersRepo;

    public WabstiCContestDetailsAttacher(
        IDbRepository<DataContext, ContestCountingCircleDetails> contestDetailsRepo,
        CountOfVotersInformationSubTotalRepo countOfVotersRepo)
    {
        _contestDetailsRepo = contestDetailsRepo;
        _countOfVotersRepo = countOfVotersRepo;
    }

    internal async Task AttachSwissAbroadCountOfVoters<T>(Guid contestId, IEnumerable<T> data, CancellationToken ct = default)
        where T : class, IWabstiCSwissAbroadCountOfVoters
    {
        var swissAbroadCountOfVoters = await _countOfVotersRepo.GetCountOfVotersByCountCircleId(
            contestId,
            VoterType.SwissAbroad,
            ct);

        foreach (var entry in data)
        {
            entry.CountOfVotersTotalSwissAbroad = swissAbroadCountOfVoters.GetValueOrDefault(entry.CountingCircleId);
        }
    }

    internal async Task AttachContestDetails<T>(Guid contestId, IEnumerable<T> data, CancellationToken ct = default)
        where T : class, IWabstiCContestDetails
    {
        // with ef core 5 includes can be filtered (ex. only include swiss abroad count of voters)
        var contestDetails = await _contestDetailsRepo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == contestId)
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .ToListAsync(ct);

        var contestDetailsByCountingCircleId = contestDetails.ToDictionary(x => x.CountingCircleId);
        foreach (var entry in data)
        {
            if (!contestDetailsByCountingCircleId.TryGetValue(entry.CountingCircleId, out var contestDetail))
            {
                continue;
            }

            entry.CountOfVotersTotalSwissAbroad = contestDetail.CountOfVotersInformationSubTotals
                .Where(x => x.VoterType == VoterType.SwissAbroad)
                .Sum(x => x.CountOfVoters.GetValueOrDefault());

            var vcByValidityAndChannel = contestDetail.VotingCards
                .Where(x => x.DomainOfInfluenceType == entry.DomainOfInfluenceType)
                .GroupBy(x => (x.Valid, x.Channel))
                .ToDictionary(x => x.Key, x => x.Sum(y => y.CountOfReceivedVotingCards.GetValueOrDefault()));
            entry.VotingCardsPaper = vcByValidityAndChannel.GetValueOrDefault((true, VotingChannel.Paper));
            entry.VotingCardsBallotBox = vcByValidityAndChannel.GetValueOrDefault((true, VotingChannel.BallotBox));
            entry.VotingCardsByMail = vcByValidityAndChannel.GetValueOrDefault((true, VotingChannel.ByMail));
            entry.VotingCardsByMailNotValid = vcByValidityAndChannel.GetValueOrDefault((false, VotingChannel.ByMail));
            entry.VotingCardsEVoting = vcByValidityAndChannel.GetValueOrDefault((true, VotingChannel.EVoting));
        }
    }
}
