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

public class MajorityElectionResultAggregate : ElectionResultAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<ElectionResultEntryParams> _resultEntryParamsValidator;
    private readonly IMapper _mapper;

    public MajorityElectionResultAggregate(
        EventInfoProvider eventInfoProvider,
        IValidator<ElectionResultEntryParams> resultEntryParamsValidator,
        IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _resultEntryParamsValidator = resultEntryParamsValidator;
        _mapper = mapper;
    }

    public override Guid PoliticalBusinessId => MajorityElectionId;

    public Guid MajorityElectionId { get; private set; }

    public MajorityElectionResultEntry ResultEntry { get; private set; }

    public MajorityElectionResultEntryParams ResultEntryParams { get; private set; } = new();

    public override string AggregateName => "voting-majorityElectionResult";

    public override void StartSubmission(Guid countingCircleId, Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded)
    {
        EnsureInState(CountingCircleResultState.Initial);
        Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(politicalBusinessId, countingCircleId, testingPhaseEnded);
        RaiseEvent(
            new MajorityElectionResultSubmissionStarted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                ElectionId = politicalBusinessId.ToString(),
                CountingCircleId = countingCircleId.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void DefineEntry(MajorityElectionResultEntry resultEntry, Guid contestId, MajorityElectionResultEntryParams? resultEntryParams = null)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing);

        if (resultEntry != MajorityElectionResultEntry.Detailed
            && resultEntry != MajorityElectionResultEntry.FinalResults)
        {
            throw new ValidationException("invalid result entry");
        }

        var isDetailedEntry = resultEntry == MajorityElectionResultEntry.Detailed;
        var hasParams = resultEntryParams != null;

        if (!isDetailedEntry && hasParams)
        {
            throw new ValidationException("can't provide details if result entry is set to final results");
        }

        if (isDetailedEntry && !hasParams)
        {
            throw new ValidationException("details are required if result entry is set to detailed");
        }

        if (hasParams)
        {
            _resultEntryParamsValidator.ValidateAndThrow(resultEntryParams!);
        }

        RaiseEvent(
            new MajorityElectionResultEntryDefined
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                ResultEntry = _mapper.Map<Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionResultEntry>(resultEntry),
                ResultEntryParams = _mapper.Map<MajorityElectionResultEntryParamsEventData>(resultEntryParams),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterCountOfVoters(PoliticalBusinessCountOfVoters countOfVoters, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);

        RaiseEvent(
            new MajorityElectionResultCountOfVotersEntered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                CountOfVoters = _mapper.Map<PoliticalBusinessCountOfVotersEventData>(countOfVoters),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterCandidateResults(
        int? individualVoteCount,
        int? emptyVoteCount,
        int? invalidVoteCount,
        IReadOnlyCollection<MajorityElectionCandidateResult> candidateResults,
        IReadOnlyCollection<SecondaryMajorityElectionCandidateResults> secondaryCandidateResults,
        Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        if (ResultEntry != MajorityElectionResultEntry.FinalResults)
        {
            throw new ValidationException("candidate results can only be entered if result entry is set to final results");
        }

        var ev = new MajorityElectionCandidateResultsEntered
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ElectionResultId = Id.ToString(),
            IndividualVoteCount = individualVoteCount,
            EmptyVoteCount = emptyVoteCount,
            InvalidVoteCount = invalidVoteCount,
        };
        _mapper.Map(candidateResults, ev.CandidateResults);
        _mapper.Map(secondaryCandidateResults, ev.SecondaryElectionCandidateResults);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterBallotGroupResults(IReadOnlyCollection<MajorityElectionBallotGroupResult> results, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        EnsureDetailedResultEntry();

        var ev = new MajorityElectionBallotGroupResultsEntered
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
        EnsureDetailedResultEntry();
        if (!ResultEntryParams.AutomaticBallotBundleNumberGeneration)
        {
            throw new ValidationException("Automatic Ballot Bundle Number Generation is not enabled");
        }

        var bundleNumber = GetNextBundleNumber();
        RaiseEvent(
            new MajorityElectionResultBundleNumberEntered
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
        if (ResultEntryParams.AutomaticBallotBundleNumberGeneration)
        {
            throw new ValidationException("Automatic Ballot Bundle Number Generation is enabled");
        }

        if (BundleNumbers.Contains(bundleNumber) && !DeletedUnusedBundleNumbers.Contains(bundleNumber))
        {
            throw new ValidationException("bundle number is already in use");
        }

        RaiseEvent(
            new MajorityElectionResultBundleNumberEntered
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
            new MajorityElectionResultBundleNumberFreed
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
            new MajorityElectionResultSubmissionFinished
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
            new MajorityElectionResultCorrectionFinished
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
            new MajorityElectionResultResettedToSubmissionFinished
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
            new MajorityElectionResultFlaggedForCorrection
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                Comment = comment,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void AuditedTentatively(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionDone, CountingCircleResultState.CorrectionDone);
        RaiseEvent(
            new MajorityElectionResultAuditedTentatively
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
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
            new MajorityElectionResultResetted
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
            new MajorityElectionResultPlausibilised
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
            new MajorityElectionResultResettedToAuditedTentatively
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
            case MajorityElectionResultSubmissionStarted ev:
                Apply(ev);
                break;
            case MajorityElectionResultEntryDefined ev:
                Apply(ev);
                break;
            case MajorityElectionResultCountOfVotersEntered ev:
                Apply(ev);
                break;
            case MajorityElectionResultBundleNumberEntered ev:
                BundleNumbers.Add(ev.BundleNumber);
                DeletedUnusedBundleNumbers.Remove(ev.BundleNumber);
                break;
            case MajorityElectionResultBundleNumberFreed ev:
                DeletedUnusedBundleNumbers.Add(ev.BundleNumber);
                break;
            case MajorityElectionResultSubmissionFinished _:
                State = CountingCircleResultState.SubmissionDone;
                break;
            case MajorityElectionResultCorrectionFinished _:
                State = CountingCircleResultState.CorrectionDone;
                break;
            case MajorityElectionResultFlaggedForCorrection _:
                State = CountingCircleResultState.ReadyForCorrection;
                break;
            case MajorityElectionResultAuditedTentatively _:
                State = CountingCircleResultState.AuditedTentatively;
                break;
            case MajorityElectionResultPlausibilised _:
                State = CountingCircleResultState.Plausibilised;
                break;
            case MajorityElectionResultResettedToAuditedTentatively _:
                State = CountingCircleResultState.AuditedTentatively;
                break;
            case MajorityElectionResultResettedToSubmissionFinished _:
                State = CountingCircleResultState.SubmissionDone;
                break;
            case MajorityElectionResultResetted _:
                State = CountingCircleResultState.SubmissionOngoing;
                break;
        }
    }

    private void Apply(MajorityElectionResultSubmissionStarted ev)
    {
        Id = GuidParser.Parse(ev.ElectionResultId);
        CountingCircleId = GuidParser.Parse(ev.CountingCircleId);
        MajorityElectionId = GuidParser.Parse(ev.ElectionId);
        State = CountingCircleResultState.SubmissionOngoing;
    }

    private void Apply(MajorityElectionResultEntryDefined ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.ResultEntryParams?.ReviewProcedure == Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionReviewProcedure.Unspecified)
        {
            ev.ResultEntryParams.ReviewProcedure = Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionReviewProcedure.Electronically;
        }

        ResultEntry = _mapper.Map<MajorityElectionResultEntry>(ev.ResultEntry);
        ResultEntryParams = _mapper.Map<MajorityElectionResultEntryParams>(ev.ResultEntryParams) ?? new MajorityElectionResultEntryParams();
        ResetBundleNumbers();
    }

    private void Apply(MajorityElectionResultCountOfVotersEntered ev)
    {
        CountOfVoters = _mapper.Map<PoliticalBusinessCountOfVoters>(ev.CountOfVoters);
    }

    private void EnsureDetailedResultEntry()
    {
        if (ResultEntry != MajorityElectionResultEntry.Detailed)
        {
            throw new ValidationException("this is only allowed for detailed result entry");
        }
    }
}
