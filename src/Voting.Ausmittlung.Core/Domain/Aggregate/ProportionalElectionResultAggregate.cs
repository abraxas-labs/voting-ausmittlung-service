// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ProportionalElectionResultAggregate : ElectionResultAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<ElectionResultEntryParams> _resultEntryParamsValidator;
    private readonly IMapper _mapper;

    public ProportionalElectionResultAggregate(
        EventInfoProvider eventInfoProvider,
        IValidator<ElectionResultEntryParams> resultEntryValidatorParamsValidator,
        IValidator<PoliticalBusinessCountOfVoters> countOfVotersValidator,
        IMapper mapper,
        EventSignatureService eventSignatureService)
        : base(countOfVotersValidator, eventSignatureService, mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _resultEntryParamsValidator = resultEntryValidatorParamsValidator;
        _mapper = mapper;
    }

    public ElectionResultEntryParams ResultEntry { get; private set; } =
        new ElectionResultEntryParams();

    public override string AggregateName => "voting-proportionalElectionResult";

    public void StartSubmission(Guid countingCircleId, Guid electionId, Guid contestId, bool testingPhaseEnded)
    {
        EnsureInState(CountingCircleResultState.Initial);
        Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(electionId, countingCircleId, testingPhaseEnded);
        RaiseEvent(
            new ProportionalElectionResultSubmissionStarted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                ElectionId = electionId.ToString(),
                CountingCircleId = countingCircleId.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    public void DefineEntry(ElectionResultEntryParams resultEntry, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing);
        _resultEntryParamsValidator.ValidateAndThrow(resultEntry);

        RaiseEvent(
            new ProportionalElectionResultEntryDefined
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                ResultEntryParams = _mapper.Map<ProportionalElectionResultEntryParamsEventData>(resultEntry),
            },
            new EventSignatureDomainData(contestId));
    }

    public void EnterCountOfVoters(PoliticalBusinessCountOfVoters countOfVoters, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        ValidateCountOfVoters(countOfVoters);

        RaiseEvent(
            new ProportionalElectionResultCountOfVotersEntered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                CountOfVoters = _mapper.Map<PoliticalBusinessCountOfVotersEventData>(countOfVoters),
            },
            new EventSignatureDomainData(contestId));
    }

    public void EnterUnmodifiedListResults(IReadOnlyCollection<ProportionalElectionUnmodifiedListResult> results, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);

        if (results.Any(x => x.VoteCount < 0))
        {
            throw new ValidationException("negative results are not allowed");
        }

        var ev = new ProportionalElectionUnmodifiedListResultsEntered
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ElectionResultId = Id.ToString(),
        };
        _mapper.Map(results, ev.Results);
        RaiseEvent(ev, new EventSignatureDomainData(contestId));
    }

    public int GenerateBundleNumber(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        if (!ResultEntry.AutomaticBallotBundleNumberGeneration)
        {
            throw new ValidationException("Automatic Ballot Bundle Number Generation is not enabled");
        }

        var bundleNumber = GetNextBundleNumber();
        RaiseEvent(
            new ProportionalElectionResultBundleNumberEntered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                BundleNumber = bundleNumber,
            },
            new EventSignatureDomainData(contestId));
        return bundleNumber;
    }

    public void BundleNumberEntered(int bundleNumber, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        if (ResultEntry.AutomaticBallotBundleNumberGeneration)
        {
            throw new ValidationException("Automatic Ballot Bundle Number Generation is enabled");
        }

        if (BundleNumbers.Contains(bundleNumber) && !DeletedUnusedBundleNumbers.Contains(bundleNumber))
        {
            throw new ValidationException("bundle number is already in use");
        }

        RaiseEvent(
            new ProportionalElectionResultBundleNumberEntered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                BundleNumber = bundleNumber,
            },
            new EventSignatureDomainData(contestId));
    }

    public void FreeBundleNumber(int bundleNumber, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        if (!BundleNumbers.Contains(bundleNumber))
        {
            throw new ValidationException("unknown bundle number");
        }

        RaiseEvent(
            new ProportionalElectionResultBundleNumberFreed
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                BundleNumber = bundleNumber,
            },
            new EventSignatureDomainData(contestId));
    }

    public ActionId PrepareSubmissionFinished()
    {
        return BuildActionId(nameof(SubmissionFinished));
    }

    public void SubmissionFinished(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing);

        RaiseEvent(
            new ProportionalElectionResultSubmissionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    public ActionId PrepareCorrectionFinished()
    {
        return BuildActionId(nameof(CorrectionFinished));
    }

    public void CorrectionFinished(string comment, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.ReadyForCorrection);

        RaiseEvent(
            new ProportionalElectionResultCorrectionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                Comment = comment,
            },
            new EventSignatureDomainData(contestId));
    }

    public void ResetToSubmissionFinished(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.AuditedTentatively);
        RaiseEvent(
            new ProportionalElectionResultResettedToSubmissionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    public override void FlagForCorrection(Guid contestId, string comment = "")
    {
        EnsureInState(CountingCircleResultState.SubmissionDone, CountingCircleResultState.CorrectionDone);
        RaiseEvent(
            new ProportionalElectionResultFlaggedForCorrection
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                Comment = comment,
            },
            new EventSignatureDomainData(contestId));
    }

    public void AuditedTentatively(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionDone, CountingCircleResultState.CorrectionDone);
        RaiseEvent(
            new ProportionalElectionResultAuditedTentatively
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    public void Plausibilise(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.AuditedTentatively);
        RaiseEvent(
            new ProportionalElectionResultPlausibilised
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    public void ResetToAuditedTentatively(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.Plausibilised);
        RaiseEvent(
            new ProportionalElectionResultResettedToAuditedTentatively
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionResultSubmissionStarted ev:
                Apply(ev);
                break;
            case ProportionalElectionResultEntryDefined ev:
                Apply(ev);
                break;
            case ProportionalElectionResultCountOfVotersEntered ev:
                Apply(ev);
                break;
            case ProportionalElectionResultBundleNumberEntered ev:
                BundleNumbers.Add(ev.BundleNumber);
                DeletedUnusedBundleNumbers.Remove(ev.BundleNumber);
                break;
            case ProportionalElectionResultBundleNumberFreed ev:
                DeletedUnusedBundleNumbers.Add(ev.BundleNumber);
                break;
            case ProportionalElectionResultSubmissionFinished _:
                State = CountingCircleResultState.SubmissionDone;
                break;
            case ProportionalElectionResultCorrectionFinished _:
                State = CountingCircleResultState.CorrectionDone;
                break;
            case ProportionalElectionResultFlaggedForCorrection _:
                State = CountingCircleResultState.ReadyForCorrection;
                break;
            case ProportionalElectionResultAuditedTentatively _:
                State = CountingCircleResultState.AuditedTentatively;
                break;
            case ProportionalElectionResultPlausibilised _:
                State = CountingCircleResultState.Plausibilised;
                break;
            case ProportionalElectionResultResettedToAuditedTentatively _:
                State = CountingCircleResultState.AuditedTentatively;
                break;
            case ProportionalElectionResultResettedToSubmissionFinished _:
                State = CountingCircleResultState.SubmissionDone;
                break;
        }
    }

    private void Apply(ProportionalElectionResultSubmissionStarted ev)
    {
        Id = GuidParser.Parse(ev.ElectionResultId);
        State = CountingCircleResultState.SubmissionOngoing;
    }

    private void Apply(ProportionalElectionResultEntryDefined ev)
    {
        ResultEntry = _mapper.Map<ElectionResultEntryParams>(ev.ResultEntryParams) ?? new ElectionResultEntryParams();
        ResetBundleNumbers();
    }

    private void Apply(ProportionalElectionResultCountOfVotersEntered ev)
    {
        CountOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(ev.CountOfVoters);
    }
}