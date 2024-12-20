﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ContestCountingCircleContactPersonProcessor :
    IEventProcessor<ContestCountingCircleContactPersonCreated>,
    IEventProcessor<ContestCountingCircleContactPersonUpdated>
{
    private readonly CountingCircleRepo _countingCircleRepo;
    private readonly CountingCircleContactPersonRepo _countingCircleContactPersonRepo;
    private readonly IMapper _mapper;

    public ContestCountingCircleContactPersonProcessor(
        IMapper mapper,
        CountingCircleRepo countingCircleRepo,
        CountingCircleContactPersonRepo countingCircleContactPersonRepo)
    {
        _countingCircleRepo = countingCircleRepo;
        _countingCircleContactPersonRepo = countingCircleContactPersonRepo;
        _mapper = mapper;
    }

    public async Task Process(ContestCountingCircleContactPersonCreated eventData)
    {
        var contactPersonId = GuidParser.Parse(eventData.ContestCountingCircleContactPersonId);
        var contestId = GuidParser.Parse(eventData.ContestId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var existing = await _countingCircleRepo.Query()
                           .Include(x => x.ContactPersonDuringEvent)
                           .Include(x => x.ContactPersonAfterEvent)
                           .Include(x => x.ResponsibleAuthority)
                           .FirstOrDefaultAsync(x => x.BasisCountingCircleId == countingCircleId && x.SnapshotContestId == contestId)
                       ?? throw new EntityNotFoundException(countingCircleId);

        existing.ContestCountingCircleContactPersonId = contactPersonId;
        existing.MustUpdateContactPersons = false;
        _mapper.Map(eventData.ContactPersonDuringEvent, existing.ContactPersonDuringEvent);
        existing.ContactPersonSameDuringEventAsAfter = eventData.ContactPersonSameDuringEventAsAfter;

        if (eventData.ContactPersonSameDuringEventAsAfter)
        {
            if (existing.ContactPersonAfterEvent != null)
            {
                await _countingCircleContactPersonRepo.DeleteByKeyIfExists(existing.ContactPersonAfterEvent.Id);
                existing.ContactPersonAfterEvent = null;
            }
        }
        else
        {
            _mapper.Map(eventData.ContactPersonAfterEvent, existing.ContactPersonAfterEvent);
        }

        await _countingCircleRepo.Update(existing);
    }

    public async Task Process(ContestCountingCircleContactPersonUpdated eventData)
    {
        var contactPersonId = GuidParser.Parse(eventData.ContestCountingCircleContactPersonId);
        var existing = await _countingCircleRepo.Query()
                           .Include(x => x.ContactPersonDuringEvent)
                           .Include(x => x.ContactPersonAfterEvent)
                           .Include(x => x.ResponsibleAuthority)
                           .FirstOrDefaultAsync(x => x.ContestCountingCircleContactPersonId == contactPersonId)
                       ?? throw new EntityNotFoundException(contactPersonId);

        existing.MustUpdateContactPersons = false;
        _mapper.Map(eventData.ContactPersonDuringEvent, existing.ContactPersonDuringEvent);
        existing.ContactPersonSameDuringEventAsAfter = eventData.ContactPersonSameDuringEventAsAfter;

        if (eventData.ContactPersonSameDuringEventAsAfter)
        {
            if (existing.ContactPersonAfterEvent != null)
            {
                await _countingCircleContactPersonRepo.DeleteByKeyIfExists(existing.ContactPersonAfterEvent.Id);
                existing.ContactPersonAfterEvent = null;
            }
        }
        else
        {
            _mapper.Map(eventData.ContactPersonAfterEvent, existing.ContactPersonAfterEvent);
        }

        await _countingCircleRepo.Update(existing);
    }
}
