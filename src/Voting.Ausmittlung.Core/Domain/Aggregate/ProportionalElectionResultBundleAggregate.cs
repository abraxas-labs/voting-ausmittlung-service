// (c) Copyright by Abraxas Informatik AG
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
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ProportionalElectionResultBundleAggregate : PoliticalBusinessResultBundleAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public ProportionalElectionResultBundleAggregate(EventInfoProvider eventInfoProvider, IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;
    }

    public override string AggregateName => "voting-proportionalElectionResultBundle";

    public Guid? ListId { get; private set; }

    public ProportionalElectionResultEntryParams ResultEntryParams { get; private set; } = new();

    protected override int BallotBundleSampleSize => ResultEntryParams.BallotBundleSampleSize;

    public void Create(
        Guid? bundleId,
        Guid electionResultId,
        Guid? listId,
        int bundleNumber,
        ProportionalElectionResultEntryParams resultEntry,
        Guid contestId)
    {
        if (bundleNumber < 1)
        {
            throw new ValidationException("bundleNumber must be greater than zero");
        }

        Id = bundleId ?? Guid.NewGuid();
        RaiseEvent(
            new ProportionalElectionResultBundleCreated
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BundleNumber = bundleNumber,
                BundleId = Id.ToString(),
                ListId = listId?.ToString() ?? string.Empty,
                ElectionResultId = electionResultId.ToString(),
                ResultEntryParams = _mapper.Map<ProportionalElectionResultEntryParamsEventData>(resultEntry),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void CreateBallot(
        int emptyVoteCount,
        IReadOnlyCollection<ProportionalElectionResultBallotCandidate> candidates,
        Guid contestId)
    {
        EnsureInState(BallotBundleState.InProcess, BallotBundleState.InCorrection);
        if (BallotNumbers.Count >= ResultEntryParams.BallotBundleSize)
        {
            throw new ValidationException("bundle size already reached");
        }

        var ev = new ProportionalElectionResultBallotCreated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            ElectionResultId = PoliticalBusinessResultId.ToString(),
            BallotNumber = CurrentBallotNumber + 1,
            EmptyVoteCount = emptyVoteCount,
        };
        _mapper.Map(candidates, ev.Candidates);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void UpdateBallot(
        int ballotNumber,
        int emptyVoteCount,
        IReadOnlyCollection<ProportionalElectionResultBallotCandidate> candidates,
        Guid contestId)
    {
        EnsureInState(BallotBundleState.InProcess, BallotBundleState.InCorrection, BallotBundleState.ReadyForReview);
        EnsureHasBallot(ballotNumber);

        var ev = new ProportionalElectionResultBallotUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            ElectionResultId = PoliticalBusinessResultId.ToString(),
            BallotNumber = ballotNumber,
            EmptyVoteCount = emptyVoteCount,
        };
        _mapper.Map(candidates, ev.Candidates);
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
            new ProportionalElectionResultBallotDeleted
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

        var ev = new ProportionalElectionResultBundleSubmissionFinished
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

        var ev = new ProportionalElectionResultBundleCorrectionFinished
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
            new ProportionalElectionResultBundleReviewRejected
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = PoliticalBusinessResultId.ToString(),
                BundleId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void SucceedReview(Guid contestId)
    {
        EnsureInState(BallotBundleState.ReadyForReview);
        RaiseEvent(
            new ProportionalElectionResultBundleReviewSucceeded
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = PoliticalBusinessResultId.ToString(),
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
            new ProportionalElectionResultBundleDeleted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ElectionResultId = PoliticalBusinessResultId.ToString(),
                BundleId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionResultBundleCreated ev:
                Apply(ev);
                break;
            case ProportionalElectionResultBallotCreated ev:
                Apply(ev);
                break;
            case ProportionalElectionResultBallotDeleted ev:
                Apply(ev);
                break;
            case ProportionalElectionResultBundleSubmissionFinished _:
            case ProportionalElectionResultBundleCorrectionFinished _:
                State = BallotBundleState.ReadyForReview;
                break;
            case ProportionalElectionResultBundleReviewRejected _:
                State = BallotBundleState.InCorrection;
                break;
            case ProportionalElectionResultBundleReviewSucceeded _:
                State = BallotBundleState.Reviewed;
                break;
            case ProportionalElectionResultBundleDeleted _:
                State = BallotBundleState.Deleted;
                break;
        }
    }

    private void Apply(ProportionalElectionResultBundleCreated ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.ResultEntryParams.ReviewProcedure == Abraxas.Voting.Ausmittlung.Shared.V1.ProportionalElectionReviewProcedure.Unspecified)
        {
            ev.ResultEntryParams.ReviewProcedure = Abraxas.Voting.Ausmittlung.Shared.V1.ProportionalElectionReviewProcedure.Electronically;
        }

        Id = GuidParser.Parse(ev.BundleId);
        PoliticalBusinessResultId = GuidParser.Parse(ev.ElectionResultId);
        CreatedBy = ev.EventInfo.User.Id;
        ListId = GuidParser.ParseNullable(ev.ListId);
        BundleNumber = ev.BundleNumber;
        ResultEntryParams = _mapper.Map<ProportionalElectionResultEntryParams>(ev.ResultEntryParams);
        CurrentBallotNumber = ResultEntryParams.BallotNumberGeneration == BallotNumberGeneration.RestartForEachBundle
            ? 0
            : (BundleNumber - 1) * ResultEntryParams.BallotBundleSize;
    }

    private void Apply(ProportionalElectionResultBallotCreated ev)
    {
        BallotNumbers.Add(ev.BallotNumber);
        CurrentBallotNumber = ev.BallotNumber;
    }

    private void Apply(ProportionalElectionResultBallotDeleted ev)
    {
        BallotNumbers.Remove(ev.BallotNumber);
        CurrentBallotNumber = ev.BallotNumber - 1;
    }
}
