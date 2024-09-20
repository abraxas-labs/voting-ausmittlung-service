// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ContestCountingCircleElectorateProcessor : IEventProcessor<ContestCountingCircleElectoratesCreated>,
    IEventProcessor<ContestCountingCircleElectoratesUpdated>
{
    private readonly IDbRepository<DataContext, ContestCountingCircleElectorate> _repo;
    private readonly IDbRepository<DataContext, VotingCardResultDetail> _vcRepo;
    private readonly IMapper _mapper;
    private readonly ContestCountingCircleDetailsBuilder _contestCountingCircleDetailsBuilder;

    public ContestCountingCircleElectorateProcessor(
        IDbRepository<DataContext, ContestCountingCircleElectorate> repo,
        IDbRepository<DataContext, VotingCardResultDetail> vcRepo,
        IMapper mapper,
        ContestCountingCircleDetailsBuilder contestCountingCircleDetailsBuilder)
    {
        _repo = repo;
        _vcRepo = vcRepo;
        _mapper = mapper;
        _contestCountingCircleDetailsBuilder = contestCountingCircleDetailsBuilder;
    }

    public Task Process(ContestCountingCircleElectoratesCreated eventData)
        => ProcessCreateUpdate(
            GuidParser.Parse(eventData.ContestId),
            GuidParser.Parse(eventData.CountingCircleId),
            _mapper.Map<List<ContestCountingCircleElectorate>>(eventData.Electorates));

    public Task Process(ContestCountingCircleElectoratesUpdated eventData)
        => ProcessCreateUpdate(
            GuidParser.Parse(eventData.ContestId),
            GuidParser.Parse(eventData.CountingCircleId),
            _mapper.Map<List<ContestCountingCircleElectorate>>(eventData.Electorates));

    private async Task ProcessCreateUpdate(
        Guid contestId,
        Guid basisCountingCircleId,
        IReadOnlyCollection<ContestCountingCircleElectorate> electorates)
    {
        var existingElectorateIds = await _repo.Query()
            .Where(e => e.ContestId == contestId && e.CountingCircle.BasisCountingCircleId == basisCountingCircleId)
            .Select(e => e.Id)
            .ToListAsync();

        await _repo.DeleteRangeByKey(existingElectorateIds);

        foreach (var electorate in electorates)
        {
            electorate.ContestId = contestId;
            electorate.CountingCircleId =
                AusmittlungUuidV5.BuildCountingCircleSnapshot(contestId, basisCountingCircleId);
        }

        await _repo.CreateRange(electorates);
        await _contestCountingCircleDetailsBuilder.ResetConventionalVotingCards(contestId, basisCountingCircleId);
    }
}
