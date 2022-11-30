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
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class VoteResultAggregate : CountingCircleResultAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<VoteBallotResults> _ballotResultsValidator;
    private readonly IMapper _mapper;

    public VoteResultAggregate(
        EventInfoProvider eventInfoProvider,
        IValidator<VoteBallotResults> ballotResultsValidator,
        IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _ballotResultsValidator = ballotResultsValidator;
        _mapper = mapper;
    }

    public override Guid PoliticalBusinessId => VoteId;

    public Guid VoteId { get; private set; }

    public VoteResultEntry ResultEntry { get; private set; }

    public VoteResultEntryParams ResultEntryParams { get; private set; } = new();

    public Dictionary<Guid, HashSet<int>> BundleNumbersByBallotResultId { get; } = new();

    public Dictionary<Guid, HashSet<int>> DeletedUnusedBundleNumbersByBallotResultId { get; } = new();

    public override string AggregateName => "voting-voteResult";

    public override void StartSubmission(Guid countingCircleId, Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded)
    {
        EnsureInState(CountingCircleResultState.Initial);
        Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(politicalBusinessId, countingCircleId, testingPhaseEnded);
        var ev = new VoteResultSubmissionStarted
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            VoteResultId = Id.ToString(),
            VoteId = politicalBusinessId.ToString(),
            CountingCircleId = countingCircleId.ToString(),
        };
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void DefineEntry(VoteResultEntry resultEntry, Guid contestId, VoteResultEntryParams? resultEntryParams = null)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing);
        if (resultEntry != VoteResultEntry.Detailed && resultEntry != VoteResultEntry.FinalResults)
        {
            throw new ValidationException("invalid result entry value");
        }

        var isDetailedEntry = resultEntry == VoteResultEntry.Detailed;
        var hasParams = resultEntryParams != null;

        if (!isDetailedEntry && hasParams)
        {
            throw new ValidationException("can't provide details if result entry is set to final results");
        }

        if (isDetailedEntry && !hasParams)
        {
            throw new ValidationException("details are required if result entry is set to detailed");
        }

        RaiseEvent(
            new VoteResultEntryDefined
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
                ResultEntry = _mapper.Map<Abraxas.Voting.Ausmittlung.Shared.V1.VoteResultEntry>(resultEntry),
                ResultEntryParams = _mapper.Map<VoteResultEntryParamsEventData>(resultEntryParams),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterCountOfVoters(IReadOnlyCollection<VoteBallotResultsCountOfVoters> resultsCountOfVoters, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);

        var ev = new VoteResultCountOfVotersEntered
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            VoteResultId = Id.ToString(),
        };

        _mapper.Map(resultsCountOfVoters, ev.ResultsCountOfVoters);

        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterCountOfVoters(IReadOnlyCollection<VoteBallotResults> results, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        ValidateAndCalculateCountOfVoters(results);

        var ev = new VoteResultCountOfVotersEntered
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            VoteResultId = Id.ToString(),
        };

        _mapper.Map(results, ev.ResultsCountOfVoters);

        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterResults(IReadOnlyCollection<VoteBallotResults> results, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing);
        ValidateResults(results);

        var ev = new VoteResultEntered
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            VoteResultId = Id.ToString(),
        };

        _mapper.Map(results, ev.Results);

        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void EnterCorrectionResults(IReadOnlyCollection<VoteBallotResults> results, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.ReadyForCorrection);
        ValidateResults(results);

        var ev = new VoteResultCorrectionEntered
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            VoteResultId = Id.ToString(),
        };

        _mapper.Map(results, ev.Results);

        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public ActionId PrepareSubmissionFinished()
    {
        return BuildActionId(nameof(SubmissionFinished));
    }

    public override void SubmissionFinished(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing);
        RaiseEvent(
            new VoteResultSubmissionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
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
            new VoteResultCorrectionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
                Comment = comment,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void ResetToSubmissionFinished(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.AuditedTentatively);
        RaiseEvent(
            new VoteResultResettedToSubmissionFinished
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
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
            new VoteResultResetted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void FlagForCorrection(Guid contestId, string comment = "")
    {
        EnsureInState(CountingCircleResultState.SubmissionDone, CountingCircleResultState.CorrectionDone);
        RaiseEvent(
            new VoteResultFlaggedForCorrection
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
                Comment = comment,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void AuditedTentatively(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionDone, CountingCircleResultState.CorrectionDone);
        RaiseEvent(
            new VoteResultAuditedTentatively
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void Plausibilise(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.AuditedTentatively);
        RaiseEvent(
            new VoteResultPlausibilised
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public override void ResetToAuditedTentatively(Guid contestId)
    {
        EnsureInState(CountingCircleResultState.Plausibilised);
        RaiseEvent(
            new VoteResultResettedToAuditedTentatively
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public int GenerateBundleNumber(Guid ballotResultId, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        EnsureDetailedResultEntry();
        if (!ResultEntryParams.AutomaticBallotBundleNumberGeneration)
        {
            throw new ValidationException("Automatic Ballot Bundle Number Generation is not enabled");
        }

        var bundleNumber = GetNextBundleNumber(ballotResultId);
        RaiseEvent(
            new VoteResultBundleNumberEntered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BallotResultId = ballotResultId.ToString(),
                BundleNumber = bundleNumber,
            },
            new EventSignatureBusinessDomainData(contestId));
        return bundleNumber;
    }

    public void BundleNumberEntered(int bundleNumber, Guid ballotResultId, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        if (ResultEntryParams.AutomaticBallotBundleNumberGeneration)
        {
            throw new ValidationException("Automatic Ballot Bundle Number Generation is enabled");
        }

        BundleNumbersByBallotResultId.TryGetValue(ballotResultId, out var bundleNumbers);
        DeletedUnusedBundleNumbersByBallotResultId.TryGetValue(ballotResultId, out var deletedUnusedBundleNumbers);

        // check if the bundle number is in use
        // this is the case uf the bundle numbers exist for this result and contain this bundle number
        // but it is not deleted
        if (bundleNumbers?.Contains(bundleNumber) == true && deletedUnusedBundleNumbers?.Contains(bundleNumber) != true)
        {
            throw new ValidationException("bundle number is already in use");
        }

        RaiseEvent(
            new VoteResultBundleNumberEntered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BallotResultId = ballotResultId.ToString(),
                BundleNumber = bundleNumber,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void FreeBundleNumber(int bundleNumber, Guid ballotResultId, Guid contestId)
    {
        EnsureInState(CountingCircleResultState.SubmissionOngoing, CountingCircleResultState.ReadyForCorrection);
        BundleNumbersByBallotResultId.TryGetValue(ballotResultId, out var bundleNumbers);

        // only existing bundle numbers can be freed
        // therefore the bundle numbers for this result have to exist and have to contain this bundle number
        if (bundleNumbers?.Contains(bundleNumber) != true)
        {
            throw new ValidationException("unknown bundle number");
        }

        RaiseEvent(
            new VoteResultBundleNumberFreed
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BallotResultId = ballotResultId.ToString(),
                BundleNumber = bundleNumber,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case VoteResultSubmissionStarted ev:
                Apply(ev);
                break;
            case VoteResultEntryDefined ev:
                Apply(ev);
                break;
            case VoteResultEntered _:
                break;
            case VoteResultBundleNumberEntered ev:
                Apply(ev);
                break;
            case VoteResultBundleNumberFreed ev:
                Apply(ev);
                break;
            case VoteResultSubmissionFinished _:
                State = CountingCircleResultState.SubmissionDone;
                break;
            case VoteResultCorrectionFinished _:
                State = CountingCircleResultState.CorrectionDone;
                break;
            case VoteResultFlaggedForCorrection _:
                State = CountingCircleResultState.ReadyForCorrection;
                break;
            case VoteResultAuditedTentatively _:
                State = CountingCircleResultState.AuditedTentatively;
                break;
            case VoteResultPlausibilised _:
                State = CountingCircleResultState.Plausibilised;
                break;
            case VoteResultResettedToAuditedTentatively _:
                State = CountingCircleResultState.AuditedTentatively;
                break;
            case VoteResultResettedToSubmissionFinished _:
                State = CountingCircleResultState.SubmissionDone;
                break;
            case VoteResultResetted _:
                State = CountingCircleResultState.SubmissionOngoing;
                break;
        }
    }

    private void Apply(VoteResultSubmissionStarted ev)
    {
        Id = GuidParser.Parse(ev.VoteResultId);
        CountingCircleId = GuidParser.Parse(ev.CountingCircleId);
        VoteId = GuidParser.Parse(ev.VoteId);
        State = CountingCircleResultState.SubmissionOngoing;
    }

    private void Apply(VoteResultEntryDefined ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.ResultEntryParams?.ReviewProcedure == Abraxas.Voting.Ausmittlung.Shared.V1.VoteReviewProcedure.Unspecified)
        {
            ev.ResultEntryParams.ReviewProcedure = Abraxas.Voting.Ausmittlung.Shared.V1.VoteReviewProcedure.Electronically;
        }

        ResultEntry = _mapper.Map<VoteResultEntry>(ev.ResultEntry);
        ResultEntryParams = _mapper.Map<VoteResultEntryParams>(ev.ResultEntryParams) ?? new VoteResultEntryParams();
        ResetBundleNumbers();
    }

    private void Apply(VoteResultBundleNumberEntered ev)
    {
        var ballotResultId = Guid.Parse(ev.BallotResultId);
        BundleNumbersByBallotResultId.TryGetValue(ballotResultId, out var bundleNumbers);
        bundleNumbers ??= new();
        bundleNumbers.Add(ev.BundleNumber);
        BundleNumbersByBallotResultId[ballotResultId] = bundleNumbers;
        DeletedUnusedBundleNumbersByBallotResultId.GetValueOrDefault(ballotResultId)?.Remove(ev.BundleNumber);
    }

    private void Apply(VoteResultBundleNumberFreed ev)
    {
        var ballotResultId = Guid.Parse(ev.BallotResultId);

        DeletedUnusedBundleNumbersByBallotResultId.TryGetValue(ballotResultId, out var deletedUnusedBundleNumbers);
        deletedUnusedBundleNumbers ??= new HashSet<int>();
        deletedUnusedBundleNumbers.Add(ev.BundleNumber);
        DeletedUnusedBundleNumbersByBallotResultId[ballotResultId] = deletedUnusedBundleNumbers;
    }

    private void ValidateResults(IEnumerable<VoteBallotResults> results)
    {
        foreach (var result in results)
        {
            _ballotResultsValidator.ValidateAndThrow(result);
        }
    }

    private void ValidateAndCalculateCountOfVoters(
        IEnumerable<VoteBallotResults> results)
    {
        foreach (var result in results)
        {
            _ballotResultsValidator.ValidateAndThrow(result);
        }
    }

    private void EnsureDetailedResultEntry()
    {
        if (ResultEntry != VoteResultEntry.Detailed)
        {
            throw new ValidationException("this is only allowed for detailed result entry");
        }
    }

    private int GetNextBundleNumber(Guid ballotResultId)
    {
        BundleNumbersByBallotResultId.TryGetValue(ballotResultId, out var bundleNumbers);
        return bundleNumbers == null || bundleNumbers.Count == 0
            ? 1
            : bundleNumbers.Max() + 1;
    }

    private void ResetBundleNumbers()
    {
        BundleNumbersByBallotResultId.Clear();
        DeletedUnusedBundleNumbersByBallotResultId.Clear();
    }
}
