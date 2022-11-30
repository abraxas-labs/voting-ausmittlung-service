// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
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
        IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _resultEntryParamsValidator = resultEntryValidatorParamsValidator;
        _mapper = mapper;
    }

    public override Guid PoliticalBusinessId => ProportionalElectionId;

    public Guid ProportionalElectionId { get; private set; }

    public ProportionalElectionResultEntryParams ResultEntry { get; private set; } = new();

    public override string AggregateName => "voting-proportionalElectionResult";

    public override void StartSubmission(Guid countingCircleId, Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded)
    {
        EnsureInState(CountingCircleResultState.Initial);
        Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(politicalBusinessId, countingCircleId, testingPhaseEnded);
        RaiseEvent(
            new ProportionalElectionResultSubmissionStarted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                ElectionId = politicalBusinessId.ToString(),
                CountingCircleId = countingCircleId.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void DefineEntry(ProportionalElectionResultEntryParams resultEntry, Guid contestId)
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
            new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterCountOfVoters(PoliticalBusinessCountOfVoters countOfVoters, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);

        RaiseEvent(
            new ProportionalElectionResultCountOfVotersEntered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                CountOfVoters = _mapper.Map<PoliticalBusinessCountOfVotersEventData>(countOfVoters),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterUnmodifiedListResults(IReadOnlyCollection<ProportionalElectionUnmodifiedListResult> results, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);

        var ev = new ProportionalElectionUnmodifiedListResultsEntered
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ElectionResultId = Id.ToString(),
        };
        _mapper.Map(results, ev.Results);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
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
            new EventSignatureBusinessDomainData(contestId));
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
            new EventSignatureBusinessDomainData(contestId));
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
            new EventSignatureBusinessDomainData(contestId));
    }

    public ActionId PrepareSubmissionFinished()
    {
        return BuildActionId(nameof(SubmissionFinished));
    }

    public override void SubmissionFinished(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing);

        RaiseEvent(
            new ProportionalElectionResultSubmissionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public ActionId PrepareCorrectionFinished()
    {
        return BuildActionId(nameof(CorrectionFinished));
    }

    public override void CorrectionFinished(string comment, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.ReadyForCorrection);

        RaiseEvent(
            new ProportionalElectionResultCorrectionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                Comment = comment,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void ResetToSubmissionFinished(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.AuditedTentatively);
        RaiseEvent(
            new ProportionalElectionResultResettedToSubmissionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
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
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void Reset(Guid contestId)
    {
        EnsureInState(
            CountingCircleResultState.SubmissionOngoing,
            CountingCircleResultState.ReadyForCorrection,
            CountingCircleResultState.SubmissionDone,
            CountingCircleResultState.CorrectionDone);

        EnsureInTestingPhase();

        RaiseEvent(
            new ProportionalElectionResultResetted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void AuditedTentatively(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionDone, CountingCircleResultState.CorrectionDone);
        RaiseEvent(
            new ProportionalElectionResultAuditedTentatively
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void Plausibilise(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.AuditedTentatively);
        RaiseEvent(
            new ProportionalElectionResultPlausibilised
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void ResetToAuditedTentatively(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.Plausibilised);
        RaiseEvent(
            new ProportionalElectionResultResettedToAuditedTentatively
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
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
            case ProportionalElectionResultResetted _:
                State = CountingCircleResultState.SubmissionOngoing;
                break;
        }
    }

    private void Apply(ProportionalElectionResultSubmissionStarted ev)
    {
        Id = GuidParser.Parse(ev.ElectionResultId);
        CountingCircleId = GuidParser.Parse(ev.CountingCircleId);
        ProportionalElectionId = GuidParser.Parse(ev.ElectionId);
        State = CountingCircleResultState.SubmissionOngoing;
    }

    private void Apply(ProportionalElectionResultEntryDefined ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.ResultEntryParams?.ReviewProcedure == Abraxas.Voting.Ausmittlung.Shared.V1.ProportionalElectionReviewProcedure.Unspecified)
        {
            ev.ResultEntryParams.ReviewProcedure = Abraxas.Voting.Ausmittlung.Shared.V1.ProportionalElectionReviewProcedure.Electronically;
        }

        ResultEntry = _mapper.Map<ProportionalElectionResultEntryParams>(ev.ResultEntryParams) ?? new ProportionalElectionResultEntryParams();
        ResetBundleNumbers();
    }

    private void Apply(ProportionalElectionResultCountOfVotersEntered ev)
    {
        CountOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(ev.CountOfVoters);
    }
}
