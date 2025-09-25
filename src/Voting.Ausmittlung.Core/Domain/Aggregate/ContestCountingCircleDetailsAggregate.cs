// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Utils;
using EventsV2 = Abraxas.Voting.Ausmittlung.Events.V2;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ContestCountingCircleDetailsAggregate : BaseEventSignatureAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<ContestCountingCircleDetails> _validator;

    public ContestCountingCircleDetailsAggregate(
        IMapper mapper,
        EventInfoProvider eventInfoProvider,
        IValidator<ContestCountingCircleDetails> validator)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _validator = validator;
    }

    public Guid ContestId { get; private set; }

    public Guid CountingCircleId { get; private set; }

    public List<CountOfVotersInformationSubTotal> CountOfVotersInformationSubTotals { get; private set; } = new();

    public List<VotingCardResultDetail> VotingCards { get; private set; } = new List<VotingCardResultDetail>();

    public override string AggregateName => "voting-contestCountingCircleDetails";

    public void CreateFrom(ContestCountingCircleDetails details, Guid contestId, Guid countingCircleId, bool testingPhaseEnded)
    {
        EnsureUniqueDetails(details);

        _validator.ValidateAndThrow(details);

        Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, countingCircleId, testingPhaseEnded);

        var createEv = new EventsV2.ContestCountingCircleDetailsCreated
        {
            Id = Id.ToString(),
            ContestId = contestId.ToString(),
            CountingCircleId = countingCircleId.ToString(),
            CountingMachine = _mapper.Map<SharedProto.CountingMachine>(details.CountingMachine),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        _mapper.Map(details.VotingCards, createEv.VotingCards);
        _mapper.Map(details.CountOfVotersInformationSubTotals, createEv.CountOfVotersInformationSubTotals);

        RaiseEvent(createEv, new EventSignatureBusinessDomainData(contestId));
    }

    public void UpdateFrom(ContestCountingCircleDetails details, Guid contestId, Guid ccId)
    {
        EnsureUniqueDetails(details);

        _validator.ValidateAndThrow(details);

        if (contestId != ContestId)
        {
            throw new ValidationException(nameof(ContestId) + " is immutable");
        }

        if (ccId != CountingCircleId)
        {
            throw new ValidationException(nameof(CountingCircleId) + " is immutable");
        }

        var ev = new EventsV2.ContestCountingCircleDetailsUpdated
        {
            Id = Id.ToString(),
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
            CountingMachine = _mapper.Map<SharedProto.CountingMachine>(details.CountingMachine),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        _mapper.Map(details.VotingCards, ev.VotingCards);
        _mapper.Map(details.CountOfVotersInformationSubTotals, ev.CountOfVotersInformationSubTotals);

        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void Reset()
    {
        EnsureInTestingPhase();

        var ev = new ContestCountingCircleDetailsResetted
        {
            Id = Id.ToString(),
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ContestCountingCircleDetailsCreated e:
                Apply(e);
                break;
            case ContestCountingCircleDetailsUpdated e:
                Apply(e);
                break;
            case ContestCountingCircleDetailsResetted _:
                ApplyReset();
                break;
            case EventsV2.ContestCountingCircleDetailsCreated e:
                Apply(e);
                break;
            case EventsV2.ContestCountingCircleDetailsUpdated e:
                Apply(e);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(ContestCountingCircleDetailsCreated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(ContestCountingCircleDetailsUpdated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(EventsV2.ContestCountingCircleDetailsCreated ev)
    {
        _mapper.Map(ev, this);
    }

    private void Apply(EventsV2.ContestCountingCircleDetailsUpdated ev)
    {
        _mapper.Map(ev, this);
    }

    private void ApplyReset()
    {
        foreach (var votingCard in VotingCards.Where(vc => vc.Channel != Data.Models.VotingChannel.EVoting))
        {
            votingCard.CountOfReceivedVotingCards = 0;
        }

        foreach (var subTotal in CountOfVotersInformationSubTotals)
        {
            subTotal.CountOfVoters = 0;
        }
    }

    private void EnsureUniqueDetails(ContestCountingCircleDetails details)
    {
        if (details.CountOfVotersInformationSubTotals
            .GroupBy(x => new { x.Sex, x.VoterType, x.DomainOfInfluenceType })
            .Any(x => x.Count() > 1))
        {
            throw new ValidationException("duplicated count of voters subtotal found");
        }

        if (details.VotingCards
            .GroupBy(x => new { x.Channel, x.Valid, x.DomainOfInfluenceType })
            .Any(x => x.Count() > 1))
        {
            throw new ValidationException("duplicated voting card details found");
        }
    }

    private void EnsureInTestingPhase()
    {
        if (Id != AusmittlungUuidV5.BuildContestCountingCircleDetails(ContestId, CountingCircleId, false))
        {
            throw new ValidationException($"Contest counting circle details {Id} is not in testing phase");
        }
    }
}
