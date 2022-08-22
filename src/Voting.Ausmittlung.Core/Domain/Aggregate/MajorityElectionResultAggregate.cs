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

public class MajorityElectionResultAggregate : ElectionResultAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<ElectionResultEntryParams> _resultEntryParamsValidator;
    private readonly IMapper _mapper;

    public MajorityElectionResultAggregate(
        EventInfoProvider eventInfoProvider,
        IValidator<PoliticalBusinessCountOfVoters> countOfVotersValidator,
        IValidator<ElectionResultEntryParams> resultEntryParamsValidator,
        IMapper mapper,
        EventSignatureService eventSignatureService)
        : base(countOfVotersValidator, eventSignatureService, mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _resultEntryParamsValidator = resultEntryParamsValidator;
        _mapper = mapper;
    }

    public MajorityElectionResultEntry ResultEntry { get; private set; }

    public ElectionResultEntryParams ResultEntryParams { get; private set; } = new();

    public override string AggregateName => "voting-majorityElectionResult";

    public void StartSubmission(Guid countingCircleId, Guid electionId, Guid contestId, bool testingPhaseEnded)
    {
        EnsureInState(CountingCircleResultState.Initial);
        Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(electionId, countingCircleId, testingPhaseEnded);
        RaiseEvent(
            new MajorityElectionResultSubmissionStarted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                ElectionId = electionId.ToString(),
                CountingCircleId = countingCircleId.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    public void DefineEntry(MajorityElectionResultEntry resultEntry, Guid contestId, ElectionResultEntryParams? resultEntryParams = null)
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
            new EventSignatureDomainData(contestId));
    }

    public void EnterCountOfVoters(PoliticalBusinessCountOfVoters countOfVoters, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        ValidateCountOfVoters(countOfVoters);

        RaiseEvent(
            new MajorityElectionResultCountOfVotersEntered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = Id.ToString(),
                CountOfVoters = _mapper.Map<PoliticalBusinessCountOfVotersEventData>(countOfVoters),
            },
            new EventSignatureDomainData(contestId));
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

        if (candidateResults.Any(x => x.VoteCount < 0)
            || secondaryCandidateResults.Any(x => x.CandidateResults.Any(y => y.VoteCount < 0)))
        {
            throw new ValidationException("candidate results can't be negative");
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
        RaiseEvent(ev, new EventSignatureDomainData(contestId));
    }

    public void EnterBallotGroupResults(IReadOnlyCollection<MajorityElectionBallotGroupResult> results, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        EnsureDetailedResultEntry();
        if (results.Any(r => r.VoteCount < 0))
        {
            throw new ValidationException("all results must not be negative");
        }

        var ev = new MajorityElectionBallotGroupResultsEntered
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
            new EventSignatureDomainData(contestId));
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
            new MajorityElectionResultBundleNumberFreed
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
            new MajorityElectionResultSubmissionFinished
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
            new MajorityElectionResultCorrectionFinished
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
            new MajorityElectionResultResettedToSubmissionFinished
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
            new MajorityElectionResultFlaggedForCorrection
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
            new MajorityElectionResultAuditedTentatively
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
            new MajorityElectionResultPlausibilised
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
            new MajorityElectionResultResettedToAuditedTentatively
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
        }
    }

    private void Apply(MajorityElectionResultSubmissionStarted ev)
    {
        Id = GuidParser.Parse(ev.ElectionResultId);
        State = CountingCircleResultState.SubmissionOngoing;
    }

    private void Apply(MajorityElectionResultEntryDefined ev)
    {
        ResultEntry = _mapper.Map<MajorityElectionResultEntry>(ev.ResultEntry);
        ResultEntryParams = _mapper.Map<ElectionResultEntryParams>(ev.ResultEntryParams) ?? new ElectionResultEntryParams();
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
