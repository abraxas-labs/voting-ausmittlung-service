// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ContestCountingCircleDetailsProcessor :
    IEventProcessor<ContestCountingCircleDetailsCreated>,
    IEventProcessor<ContestCountingCircleDetailsUpdated>,
    IEventProcessor<ContestCountingCircleOptionsUpdated>,
    IEventProcessor<ContestCountingCircleDetailsResetted>
{
    private readonly ILogger<ContestCountingCircleDetailsProcessor> _logger;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _repo;
    private readonly IDbRepository<DataContext, CountOfVotersInformationSubTotal> _countOfVotersInformationSubTotalRepo;
    private readonly IDbRepository<DataContext, VotingCardResultDetail> _votingCardResultDetailRepo;
    private readonly VoteResultRepo _voteResultRepo;
    private readonly ProportionalElectionResultRepo _proportionalElectionResultRepo;
    private readonly MajorityElectionResultRepo _majorityElectionResultRepo;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepo;
    private readonly AggregatedContestCountingCircleDetailsBuilder _aggregatedContestCountingCircleDetailsBuilder;
    private readonly IMapper _mapper;

    public ContestCountingCircleDetailsProcessor(
        ILogger<ContestCountingCircleDetailsProcessor> logger,
        IMapper mapper,
        IDbRepository<DataContext, ContestCountingCircleDetails> repo,
        IDbRepository<DataContext, VotingCardResultDetail> votingCardResultDetailRepo,
        IDbRepository<DataContext, CountOfVotersInformationSubTotal> countOfVotersInformationSubTotalRepo,
        VoteResultRepo voteResultRepo,
        ProportionalElectionResultRepo proportionalElectionResultRepo,
        MajorityElectionResultRepo majorityElectionResultRepo,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        AggregatedContestCountingCircleDetailsBuilder aggregatedContestCountingCircleDetailsBuilder)
    {
        _logger = logger;
        _repo = repo;
        _mapper = mapper;
        _votingCardResultDetailRepo = votingCardResultDetailRepo;
        _countOfVotersInformationSubTotalRepo = countOfVotersInformationSubTotalRepo;
        _voteResultRepo = voteResultRepo;
        _proportionalElectionResultRepo = proportionalElectionResultRepo;
        _majorityElectionResultRepo = majorityElectionResultRepo;
        _protocolExportRepo = protocolExportRepo;
        _aggregatedContestCountingCircleDetailsBuilder = aggregatedContestCountingCircleDetailsBuilder;
    }

    public Task Process(ContestCountingCircleDetailsCreated eventData)
        => ProcessCreateUpdate(eventData, eventData.Id);

    public Task Process(ContestCountingCircleDetailsUpdated eventData)
        => ProcessCreateUpdate(eventData, eventData.Id);

    public async Task Process(ContestCountingCircleOptionsUpdated eventData)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);
        var ccDetails = await _repo.Query()
            .AsTracking()
            .Where(x => x.ContestId == contestId)
            .Include(x => x.CountingCircle)
            .ToListAsync();
        var ccDetailsByBasisId = ccDetails.ToDictionary(x => x.CountingCircle.BasisCountingCircleId);
        foreach (var option in eventData.Options)
        {
            if (ccDetailsByBasisId.TryGetValue(GuidParser.Parse(option.CountingCircleId), out var details))
            {
                details.EVoting = option.EVoting;
            }
        }

        await _repo.UpdateRange(ccDetails);
        _logger.LogInformation("Updated contest counting circle options for contest {ContestId} from Basis event", contestId);
    }

    public async Task Process(ContestCountingCircleDetailsResetted eventData)
    {
        var id = GuidParser.Parse(eventData.Id);

        // The counting circle cannot modify e-voting data. Only load conventional data to ensure that only conventional data is reset.
        var details = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards.Where(vc => vc.Channel != VotingChannel.EVoting))
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(details, true);
        await DeleteProtocolExports(GuidParser.Parse(eventData.ContestId), GuidParser.Parse(eventData.CountingCircleId));
        ResetDetails(details);
        await _repo.Update(details);
        await UpdateCountOfVotersForCountingCircleResults(details, true);
    }

    private async Task ProcessCreateUpdate<T>(T eventData, string idStr)
        where T : IMessage<T>
    {
        var id = GuidParser.Parse(idStr);

        // The counting circle cannot modify e-voting data. Only load conventional data to ensure that only conventional data is modified.
        // However, for backwards compatibility, we must still allow it when e-voting data has been set, to support old cases where this happened.
        var mapped = _mapper.Map<ContestCountingCircleDetails>(eventData);
        Expression<Func<ContestCountingCircleDetails, IEnumerable<VotingCardResultDetail>>> votingCardExp =
            mapped.VotingCards.Any(x => x.Channel == VotingChannel.EVoting)
            ? x => x.VotingCards
            : x => x.VotingCards.Where(vc => vc.Channel != VotingChannel.EVoting);

        var details = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(votingCardExp)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(details, true);

        await _countOfVotersInformationSubTotalRepo.DeleteRangeByKey(
            details.CountOfVotersInformationSubTotals.Select(x => x.Id));

        await _votingCardResultDetailRepo.DeleteRangeByKey(details.VotingCards.Select(x => x.Id));

        _mapper.Map(eventData, details);
        details.CountingCircleId = AusmittlungUuidV5.BuildCountingCircleSnapshot(details.ContestId, details.CountingCircleId);

        await _repo.Update(details);
        await UpdateCountOfVotersForCountingCircleResults(details, false);
        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(details, false);
    }

    private async Task UpdateCountOfVotersForCountingCircleResults(ContestCountingCircleDetails details, bool isReset)
    {
        await UpdateCountOfVotersForVoteResults(details, isReset);
        await UpdateCountOfVotersForProportionalElectionResults(details, isReset);
        await UpdateCountOfVotersForMajorityElectionResults(details, isReset);
    }

    private async Task UpdateCountOfVotersForVoteResults(ContestCountingCircleDetails details, bool isReset)
    {
        var voteResults = await _voteResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.DomainOfInfluence)
            .Include(x => x.Results).ThenInclude(br => br.CountOfVoters)
            .Where(vr => vr.CountingCircleId == details.CountingCircleId)
            .ToListAsync();

        foreach (var voteResult in voteResults)
        {
            UpdateCountOfVoters(voteResult, details, isReset);
            foreach (var ballotResult in voteResult.Results)
            {
                UpdateVoterParticipation(ballotResult.CountOfVoters, details);
            }

            // ensures that the doi is not tracked multiple times.
            voteResult.Vote.DomainOfInfluence = null!;
        }

        await _voteResultRepo.UpdateRange(voteResults);
    }

    private async Task UpdateCountOfVotersForProportionalElectionResults(ContestCountingCircleDetails details, bool isReset)
    {
        var results = await _proportionalElectionResultRepo.Query()
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.CountOfVoters)
            .Where(vr => vr.CountingCircleId == details.CountingCircleId)
            .ToListAsync();

        foreach (var result in results)
        {
            UpdateCountOfVoters(result, details, isReset);
            UpdateVoterParticipation(result.CountOfVoters, details);

            // ensures that the doi is not tracked multiple times.
            result.ProportionalElection.DomainOfInfluence = null!;
        }

        await _proportionalElectionResultRepo.UpdateRange(results);
    }

    private async Task UpdateCountOfVotersForMajorityElectionResults(ContestCountingCircleDetails details, bool isReset)
    {
        var results = await _majorityElectionResultRepo.Query()
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.CountOfVoters)
            .Where(vr => vr.CountingCircleId == details.CountingCircleId)
            .ToListAsync();

        foreach (var result in results)
        {
            UpdateCountOfVoters(result, details, isReset);
            UpdateVoterParticipation(result.CountOfVoters, details);

            // ensures that the doi is not tracked multiple times.
            result.MajorityElection.DomainOfInfluence = null!;
        }

        await _majorityElectionResultRepo.UpdateRange(results);
    }

    private void UpdateCountOfVoters(CountingCircleResult result, ContestCountingCircleDetails details, bool isReset)
    {
        if (result.State != CountingCircleResultState.Initial
            && result.State != CountingCircleResultState.SubmissionOngoing
            && result.State != CountingCircleResultState.ReadyForCorrection
            && !isReset)
        {
            // There may be rare race conditions, where an election admin updates the count of voters
            // while another finishes the submission of a political business.
            throw new ContestCountingCircleDetailsNotUpdatableException();
        }

        if (result.PoliticalBusiness.SwissAbroadVotingRight == SwissAbroadVotingRight.OnEveryCountingCircle)
        {
            result.TotalCountOfVoters = details.TotalCountOfVoters;
        }
        else
        {
            result.TotalCountOfVoters = details.CountOfVotersInformationSubTotals
                .Where(x => x.VoterType == VoterType.Swiss)
                .Sum(x => x.CountOfVoters.GetValueOrDefault());
        }
    }

    private void UpdateVoterParticipation(PoliticalBusinessNullableCountOfVoters countOfVoters, ContestCountingCircleDetails details)
        => countOfVoters.UpdateVoterParticipation(details.TotalCountOfVoters);

    private void ResetDetails(ContestCountingCircleDetails details)
    {
        details.TotalCountOfVoters = 0;
        details.CountingMachine = CountingMachine.Unspecified;

        foreach (var votingCard in details.VotingCards)
        {
            votingCard.CountOfReceivedVotingCards = null;
        }

        foreach (var subTotal in details.CountOfVotersInformationSubTotals)
        {
            subTotal.CountOfVoters = null;
        }
    }

    private async Task DeleteProtocolExports(Guid contestId, Guid basisCcId)
    {
        var protocolExportIds = await _protocolExportRepo.Query()
            .Where(x => x.ContestId == contestId && x.CountingCircle!.BasisCountingCircleId == basisCcId)
            .Select(x => x.Id)
            .ToListAsync();

        await _protocolExportRepo.DeleteRangeByKey(protocolExportIds);
    }
}
