// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ContestCountingCircleElectoratesAggregate : BaseEventSignatureAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;

    public ContestCountingCircleElectoratesAggregate(IMapper mapper, EventInfoProvider eventInfoProvider)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
    }

    public Guid ContestId { get; private set; }

    public Guid CountingCircleId { get; private set; }

    public override string AggregateName => "voting-contestCountingCircleElectorates";

    public void CreateFrom(
        IReadOnlyCollection<ContestCountingCircleElectorate> electorates,
        Guid contestId,
        Guid countingCircleId)
    {
        Id = AusmittlungUuidV5.BuildCountingCircleSnapshot(contestId, countingCircleId);
        ValidateAndPrepareElectorates(contestId, countingCircleId, electorates);

        var ev = new ContestCountingCircleElectoratesCreated()
        {
            Id = Id.ToString(),
            ContestId = contestId.ToString(),
            CountingCircleId = countingCircleId.ToString(),
            Electorates = { _mapper.Map<List<ContestCountingCircleElectorateEventData>>(electorates) },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void UpdateFrom(
        IReadOnlyCollection<ContestCountingCircleElectorate> electorates,
        Guid contestId,
        Guid ccId)
    {
        if (contestId != ContestId)
        {
            throw new ValidationException(nameof(ContestId) + " is immutable");
        }

        if (ccId != CountingCircleId)
        {
            throw new ValidationException(nameof(CountingCircleId) + " is immutable");
        }

        ValidateAndPrepareElectorates(contestId, ccId, electorates);

        var ev = new ContestCountingCircleElectoratesUpdated()
        {
            Id = Id.ToString(),
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
            Electorates = { _mapper.Map<List<ContestCountingCircleElectorateEventData>>(electorates) },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ContestCountingCircleElectoratesCreated e:
                Apply(e);
                break;
            case ContestCountingCircleElectoratesUpdated e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(ContestCountingCircleElectoratesCreated e)
    {
        Id = Guid.Parse(e.Id);
        ContestId = Guid.Parse(e.ContestId);
        CountingCircleId = Guid.Parse(e.CountingCircleId);
    }

    private void Apply(ContestCountingCircleElectoratesUpdated e)
    {
        Id = Guid.Parse(e.Id);
        ContestId = Guid.Parse(e.ContestId);
        CountingCircleId = Guid.Parse(e.CountingCircleId);
    }

    private void ValidateAndPrepareElectorates(
        Guid contestId,
        Guid countingCircleId,
        IReadOnlyCollection<ContestCountingCircleElectorate> electorates)
    {
        foreach (var electorate in electorates)
        {
            electorate.DomainOfInfluenceTypes = electorate.DomainOfInfluenceTypes.OrderBy(x => x).ToList();
            electorate.Id = AusmittlungUuidV5.BuildContestCountingCircleElectorate(contestId, countingCircleId, electorate.DomainOfInfluenceTypes);
        }

        var electorateDoiTypes = electorates.SelectMany(e => e.DomainOfInfluenceTypes).ToList();

        if (electorates.Any(e => e.DomainOfInfluenceTypes.Count == 0))
        {
            throw new ValidationException("Cannot create an electorate without a domain of influence type");
        }

        if (electorateDoiTypes.Count != electorateDoiTypes.Distinct().Count())
        {
            throw new ValidationException(
                "A domain of influence type in an electorate must be unique per counting circle");
        }
    }
}
