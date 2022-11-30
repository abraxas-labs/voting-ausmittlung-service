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
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class MajorityElectionResultBundleAggregate : PoliticalBusinessResultBundleAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public MajorityElectionResultBundleAggregate(EventInfoProvider eventInfoProvider, IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;
    }

    public override string AggregateName => "voting-majorityElectionResultBundle";

    public MajorityElectionResultEntryParams ResultEntryParams { get; private set; } = new();

    protected override int BallotBundleSampleSize => ResultEntryParams.BallotBundleSampleSize;

    public void Create(
        Guid? bundleId,
        Guid electionResultId,
        int bundleNumber,
        MajorityElectionResultEntry resultEntry,
        MajorityElectionResultEntryParams? resultEntryParams,
        Guid contestId)
    {
        if (resultEntry != MajorityElectionResultEntry.Detailed || resultEntryParams == null)
        {
            throw new ValidationException("bundles can only be created for detailed result entry");
        }

        if (bundleNumber < 1)
        {
            throw new ValidationException("bundleNumber must be greater than zero");
        }

        Id = bundleId ?? Guid.NewGuid();
        RaiseEvent(
            new MajorityElectionResultBundleCreated
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BundleNumber = bundleNumber,
                BundleId = Id.ToString(),
                ElectionResultId = electionResultId.ToString(),
                ResultEntry = _mapper.Map<Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionResultEntry>(resultEntry),
                ResultEntryParams = _mapper.Map<MajorityElectionResultEntryParamsEventData>(resultEntryParams),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void CreateBallot(
        int emptyVoteCount,
        int individualVoteCount,
        int invalidVoteCount,
        IEnumerable<Guid> selectedCandidateIds,
        IEnumerable<SecondaryMajorityElectionResultBallot> secondaryResultBallots,
        Guid contestId)
    {
        EnsureInState(BallotBundleState.InProcess, BallotBundleState.InCorrection);
        if (BallotNumbers.Count >= ResultEntryParams.BallotBundleSize)
        {
            throw new ValidationException("bundle size already reached");
        }

        var ev = new MajorityElectionResultBallotCreated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            ElectionResultId = PoliticalBusinessResultId.ToString(),
            BallotNumber = CurrentBallotNumber + 1,
            EmptyVoteCount = emptyVoteCount,
            IndividualVoteCount = individualVoteCount,
            InvalidVoteCount = invalidVoteCount,
            SelectedCandidateIds =
                {
                    selectedCandidateIds.Select(x => x.ToString()),
                },
        };
        _mapper.Map(secondaryResultBallots, ev.SecondaryMajorityElectionResults);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void UpdateBallot(
        int ballotNumber,
        int emptyVoteCount,
        int individualVoteCount,
        int invalidVoteCount,
        IEnumerable<Guid> selectedCandidateIds,
        IEnumerable<SecondaryMajorityElectionResultBallot> secondaryResultBallots,
        Guid contestId)
    {
        EnsureInState(BallotBundleState.InProcess, BallotBundleState.InCorrection, BallotBundleState.ReadyForReview);
        EnsureHasBallot(ballotNumber);

        if (individualVoteCount < 0)
        {
            throw new ValidationException($"{nameof(individualVoteCount)} can't be negative");
        }

        if (emptyVoteCount < 0)
        {
            throw new ValidationException($"{nameof(emptyVoteCount)} can't be negative");
        }

        if (invalidVoteCount < 0)
        {
            throw new ValidationException($"{nameof(invalidVoteCount)} can't be negative");
        }

        var ev = new MajorityElectionResultBallotUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            ElectionResultId = PoliticalBusinessResultId.ToString(),
            BallotNumber = ballotNumber,
            EmptyVoteCount = emptyVoteCount,
            IndividualVoteCount = individualVoteCount,
            InvalidVoteCount = invalidVoteCount,
            SelectedCandidateIds =
                {
                    selectedCandidateIds.Select(x => x.ToString()),
                },
        };
        _mapper.Map(secondaryResultBallots, ev.SecondaryMajorityElectionResults);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void DeleteBallot(int ballotNumber, Guid contestId)
    {
        EnsureInState(BallotBundleState.InProcess, BallotBundleState.InCorrection);
        if (CurrentBallotNumber != ballotNumber)
        {
            throw new ValidationException("only the last ballot can be deleted");
        }

        RaiseEvent(
            new MajorityElectionResultBallotDeleted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BundleId = Id.ToString(),
                ElectionResultId = PoliticalBusinessResultId.ToString(),
                BallotNumber = ballotNumber,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void SubmissionFinished(Guid contestId)
    {
        EnsureInState(BallotBundleState.InProcess);
        if (BallotNumbers.Count == 0)
        {
            throw new ValidationException("at least one ballot is required to close this bundle");
        }

        var ev = new MajorityElectionResultBundleSubmissionFinished
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            ElectionResultId = PoliticalBusinessResultId.ToString(),
        };
        ev.SampleBallotNumbers.AddRange(GenerateBallotNumberSamples());
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void CorrectionFinished(Guid contestId)
    {
        EnsureInState(BallotBundleState.InCorrection);
        if (BallotNumbers.Count == 0)
        {
            throw new ValidationException("at least one ballot is required to close this bundle");
        }

        var ev = new MajorityElectionResultBundleCorrectionFinished
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            ElectionResultId = PoliticalBusinessResultId.ToString(),
        };
        ev.SampleBallotNumbers.AddRange(GenerateBallotNumberSamples());
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void RejectReview(Guid contestId)
    {
        EnsureInState(BallotBundleState.ReadyForReview);
        RaiseEvent(
            new MajorityElectionResultBundleReviewRejected
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BundleId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void SucceedReview(Guid contestId)
    {
        EnsureInState(BallotBundleState.ReadyForReview);
        RaiseEvent(
            new MajorityElectionResultBundleReviewSucceeded
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BundleId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void Delete(Guid contestId)
    {
        if (State == BallotBundleState.Deleted)
        {
            throw new ValidationException("bundle is already deleted");
        }

        RaiseEvent(
            new MajorityElectionResultBundleDeleted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BundleId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case MajorityElectionResultBundleCreated ev:
                Apply(ev);
                break;
            case MajorityElectionResultBallotCreated ev:
                Apply(ev);
                break;
            case MajorityElectionResultBallotDeleted ev:
                Apply(ev);
                break;
            case MajorityElectionResultBundleSubmissionFinished _:
            case MajorityElectionResultBundleCorrectionFinished _:
                State = BallotBundleState.ReadyForReview;
                break;
            case MajorityElectionResultBundleReviewRejected _:
                State = BallotBundleState.InCorrection;
                break;
            case MajorityElectionResultBundleReviewSucceeded _:
                State = BallotBundleState.Reviewed;
                break;
            case MajorityElectionResultBundleDeleted _:
                State = BallotBundleState.Deleted;
                break;
        }
    }

    private void Apply(MajorityElectionResultBundleCreated ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.ResultEntryParams.ReviewProcedure == Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionReviewProcedure.Unspecified)
        {
            ev.ResultEntryParams.ReviewProcedure = Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionReviewProcedure.Electronically;
        }

        Id = GuidParser.Parse(ev.BundleId);
        PoliticalBusinessResultId = GuidParser.Parse(ev.ElectionResultId);
        CreatedBy = ev.EventInfo.User.Id;
        BundleNumber = ev.BundleNumber;
        ResultEntryParams = _mapper.Map<MajorityElectionResultEntryParams>(ev.ResultEntryParams);
        CurrentBallotNumber = ResultEntryParams.BallotNumberGeneration == BallotNumberGeneration.RestartForEachBundle
            ? 0
            : (BundleNumber - 1) * ResultEntryParams.BallotBundleSize;
    }

    private void Apply(MajorityElectionResultBallotCreated ev)
    {
        BallotNumbers.Add(ev.BallotNumber);
        CurrentBallotNumber = ev.BallotNumber;
    }

    private void Apply(MajorityElectionResultBallotDeleted ev)
    {
        BallotNumbers.Remove(ev.BallotNumber);
        CurrentBallotNumber = ev.BallotNumber - 1;
    }
}
