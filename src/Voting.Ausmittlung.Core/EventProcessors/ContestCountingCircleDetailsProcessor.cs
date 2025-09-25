// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ContestCountingCircleDetailsProcessor :
    IEventProcessor<ContestCountingCircleDetailsCreated>,
    IEventProcessor<ContestCountingCircleDetailsUpdated>,
    IEventProcessor<Abraxas.Voting.Ausmittlung.Events.V2.ContestCountingCircleDetailsCreated>,
    IEventProcessor<Abraxas.Voting.Ausmittlung.Events.V2.ContestCountingCircleDetailsUpdated>,
#pragma warning disable CS0612 // contest counting circle options are deprecated
    IEventProcessor<ContestCountingCircleOptionsUpdated>,
#pragma warning restore CS0612
    IEventProcessor<ContestCountingCircleDetailsResetted>
{
    private readonly ILogger<ContestCountingCircleDetailsProcessor> _logger;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _repo;
    private readonly IDbRepository<DataContext, CountOfVotersInformationSubTotal> _countOfVotersInformationSubTotalRepo;
    private readonly IDbRepository<DataContext, VotingCardResultDetail> _votingCardResultDetailRepo;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepo;
    private readonly AggregatedContestCountingCircleDetailsBuilder _aggregatedContestCountingCircleDetailsBuilder;
    private readonly IMapper _mapper;
    private readonly CountingCircleResultBuilder _ccResultBuilder;
    private readonly EndResultBuilder _endResultBuilder;

    public ContestCountingCircleDetailsProcessor(
        ILogger<ContestCountingCircleDetailsProcessor> logger,
        IMapper mapper,
        IDbRepository<DataContext, ContestCountingCircleDetails> repo,
        IDbRepository<DataContext, VotingCardResultDetail> votingCardResultDetailRepo,
        IDbRepository<DataContext, CountOfVotersInformationSubTotal> countOfVotersInformationSubTotalRepo,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        AggregatedContestCountingCircleDetailsBuilder aggregatedContestCountingCircleDetailsBuilder,
        CountingCircleResultBuilder ccResultBuilder,
        EndResultBuilder endResultBuilder)
    {
        _logger = logger;
        _repo = repo;
        _mapper = mapper;
        _votingCardResultDetailRepo = votingCardResultDetailRepo;
        _countOfVotersInformationSubTotalRepo = countOfVotersInformationSubTotalRepo;
        _protocolExportRepo = protocolExportRepo;
        _aggregatedContestCountingCircleDetailsBuilder = aggregatedContestCountingCircleDetailsBuilder;
        _ccResultBuilder = ccResultBuilder;
        _endResultBuilder = endResultBuilder;
    }

    public Task Process(ContestCountingCircleDetailsCreated eventData)
        => ProcessCreateUpdate(eventData, eventData.Id, true);

    public Task Process(ContestCountingCircleDetailsUpdated eventData)
        => ProcessCreateUpdate(eventData, eventData.Id, true);

    [Obsolete("contest counting circle options are deprecated")]
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

        // End results need to be adjusted during "ContestCountingCircleDetailsResetted"
        // and not during political business resets ex: "VoteResultResetted",
        // because the related details info does not exist anymore, after this event is processed.
        await _endResultBuilder.AdjustEndResultsForCountingCircleDetailsReset(details);
        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(details, true);

        await DeleteProtocolExports(GuidParser.Parse(eventData.ContestId), GuidParser.Parse(eventData.CountingCircleId));
        ResetDetails(details);
        await _repo.Update(details);
        await _ccResultBuilder.UpdateCountOfVotersForCountingCircleResults(details, true);
    }

    public Task Process(Abraxas.Voting.Ausmittlung.Events.V2.ContestCountingCircleDetailsCreated eventData)
    {
        return ProcessCreateUpdate(eventData, eventData.Id);
    }

    public Task Process(Abraxas.Voting.Ausmittlung.Events.V2.ContestCountingCircleDetailsUpdated eventData)
    {
        return ProcessCreateUpdate(eventData, eventData.Id);
    }

    private async Task ProcessCreateUpdate<T>(T eventData, string idStr, bool isV1Event = false)
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

        await _countOfVotersInformationSubTotalRepo.DeleteRangeByKey(details.CountOfVotersInformationSubTotals.Select(x => x.Id));
        await _votingCardResultDetailRepo.DeleteRangeByKey(details.VotingCards.Select(x => x.Id));

        _mapper.Map(eventData, details);
        details.CountingCircleId = AusmittlungUuidV5.BuildCountingCircleSnapshot(details.ContestId, details.CountingCircleId);

        if (isV1Event)
        {
            MigrateV1Details(details);
        }

        await _repo.Update(details);
        await _ccResultBuilder.UpdateCountOfVotersForCountingCircleResults(details, false);
    }

    private void ResetDetails(ContestCountingCircleDetails details)
    {
        details.CountingMachine = CountingMachine.Unspecified;
        details.ResetVotingCardsAndSubTotals();
    }

    private async Task DeleteProtocolExports(Guid contestId, Guid basisCcId)
    {
        var protocolExportIds = await _protocolExportRepo.Query()
            .Where(x => x.ContestId == contestId && x.CountingCircle!.BasisCountingCircleId == basisCcId)
            .Select(x => x.Id)
            .ToListAsync();

        await _protocolExportRepo.DeleteRangeByKey(protocolExportIds);
    }

    private void MigrateV1Details(ContestCountingCircleDetails details)
    {
        var doiTypes = details.VotingCards.Select(vc => vc.DomainOfInfluenceType).ToHashSet();
        var countOfVotersBySexAndVoterType = details.CountOfVotersInformationSubTotals.ToDictionary(
            x => (x.Sex, x.VoterType),
            x => x.CountOfVoters);

        // V1 Details have CountOfVotersInformationSubTotals with a Unspecified doi type.
        // In this migration we map each V1 sub total to several V2 sub total since V2 contains a doi type.
        details.CountOfVotersInformationSubTotals = new List<CountOfVotersInformationSubTotal>();

        foreach (var countOfVoters in countOfVotersBySexAndVoterType)
        {
            foreach (var doiType in doiTypes)
            {
                details.CountOfVotersInformationSubTotals.Add(new CountOfVotersInformationSubTotal
                {
                    DomainOfInfluenceType = doiType,
                    Sex = countOfVoters.Key.Sex,
                    VoterType = countOfVoters.Key.VoterType,
                    CountOfVoters = countOfVoters.Value,
                });
            }
        }
    }
}
