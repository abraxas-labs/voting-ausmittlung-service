// (c) Copyright 2024 by Abraxas Informatik AG
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

public class VoteResultBundleAggregate : PoliticalBusinessResultBundleAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public VoteResultBundleAggregate(EventInfoProvider eventInfoProvider, IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;
    }

    public override string AggregateName => "voting-ballotResultBundle";

    public Guid BallotResultId { get; private set; }

    public VoteResultEntryParams ResultEntryParams { get; private set; } = new();

    protected override int BallotBundleSampleSize => Convert.ToInt32(Math.Ceiling(ResultEntryParams.BallotBundleSampleSizePercent / 100.0 * BallotNumbers.Count));

    public void Create(
        Guid? bundleId,
        Guid voteResultId,
        Guid ballotResultId,
        int bundleNumber,
        VoteResultEntryParams? resultEntryParams,
        Guid contestId)
    {
        if (resultEntryParams == null)
        {
            throw new ValidationException("bundles can only be created with detailed result entry params");
        }

        if (bundleNumber < 1)
        {
            throw new ValidationException("bundleNumber must be greater than zero");
        }

        Id = bundleId ?? Guid.NewGuid();
        RaiseEvent(
            new VoteResultBundleCreated
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BundleNumber = bundleNumber,
                BundleId = Id.ToString(),
                VoteResultId = voteResultId.ToString(),
                BallotResultId = ballotResultId.ToString(),
                ResultEntryParams = _mapper.Map<VoteResultEntryParamsEventData>(resultEntryParams),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void CreateBallot(
        ICollection<VoteResultBallotQuestionAnswer> questionBallotAnswers,
        ICollection<VoteResultBallotTieBreakQuestionAnswer> tieBreakQuestionBallotAnswers,
        Guid contestId)
    {
        EnsureInState(BallotBundleState.InProcess, BallotBundleState.InCorrection);

        ValidateAtLeastOneAnswer(questionBallotAnswers, tieBreakQuestionBallotAnswers);

        var ev = new VoteResultBallotCreated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            BallotResultId = BallotResultId.ToString(),
            BallotNumber = CurrentBallotNumber + 1,
        };
        _mapper.Map(questionBallotAnswers, ev.QuestionAnswers);
        _mapper.Map(tieBreakQuestionBallotAnswers, ev.TieBreakQuestionAnswers);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void UpdateBallot(
        int ballotNumber,
        ICollection<VoteResultBallotQuestionAnswer> questionBallotAnswers,
        ICollection<VoteResultBallotTieBreakQuestionAnswer> tieBreakQuestionBallotAnswers,
        Guid contestId)
    {
        EnsureInState(BallotBundleState.InProcess, BallotBundleState.InCorrection, BallotBundleState.ReadyForReview);
        EnsureHasBallot(ballotNumber);

        ValidateAtLeastOneAnswer(questionBallotAnswers, tieBreakQuestionBallotAnswers);

        var ev = new VoteResultBallotUpdated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            BallotResultId = BallotResultId.ToString(),
            BallotNumber = ballotNumber,
        };
        _mapper.Map(questionBallotAnswers, ev.QuestionAnswers);
        _mapper.Map(tieBreakQuestionBallotAnswers, ev.TieBreakQuestionAnswers);
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
            new VoteResultBallotDeleted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                BundleId = Id.ToString(),
                BallotResultId = BallotResultId.ToString(),
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

        var ev = new VoteResultBundleSubmissionFinished
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            BallotResultId = BallotResultId.ToString(),
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

        var ev = new VoteResultBundleCorrectionFinished
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            BundleId = Id.ToString(),
            BallotResultId = BallotResultId.ToString(),
        };
        ev.SampleBallotNumbers.AddRange(GenerateBallotNumberSamples());
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void RejectReview(Guid contestId)
    {
        EnsureInState(BallotBundleState.ReadyForReview);
        RaiseEvent(
            new VoteResultBundleReviewRejected
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
            new VoteResultBundleReviewSucceeded
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
            new VoteResultBundleDeleted
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
            case VoteResultBundleCreated ev:
                Apply(ev);
                break;
            case VoteResultBallotCreated ev:
                Apply(ev);
                break;
            case VoteResultBallotDeleted ev:
                Apply(ev);
                break;
            case VoteResultBundleSubmissionFinished _:
            case VoteResultBundleCorrectionFinished _:
                State = BallotBundleState.ReadyForReview;
                break;
            case VoteResultBundleReviewRejected _:
                State = BallotBundleState.InCorrection;
                break;
            case VoteResultBundleReviewSucceeded _:
                State = BallotBundleState.Reviewed;
                break;
            case VoteResultBundleDeleted _:
                State = BallotBundleState.Deleted;
                break;
        }
    }

    private void Apply(VoteResultBundleCreated ev)
    {
        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (ev.ResultEntryParams.ReviewProcedure == Abraxas.Voting.Ausmittlung.Shared.V1.VoteReviewProcedure.Unspecified)
        {
            ev.ResultEntryParams.ReviewProcedure = Abraxas.Voting.Ausmittlung.Shared.V1.VoteReviewProcedure.Electronically;
        }

        Id = GuidParser.Parse(ev.BundleId);
        PoliticalBusinessResultId = GuidParser.Parse(ev.VoteResultId);
        BallotResultId = GuidParser.Parse(ev.BallotResultId);
        CreatedBy = ev.EventInfo.User.Id;
        BundleNumber = ev.BundleNumber;
        ResultEntryParams = _mapper.Map<VoteResultEntryParams>(ev.ResultEntryParams);
        CurrentBallotNumber = 0;
    }

    private void Apply(VoteResultBallotCreated ev)
    {
        BallotNumbers.Add(ev.BallotNumber);
        CurrentBallotNumber = ev.BallotNumber;
    }

    private void Apply(VoteResultBallotDeleted ev)
    {
        BallotNumbers.Remove(ev.BallotNumber);
        CurrentBallotNumber = ev.BallotNumber - 1;
    }

    private void ValidateAtLeastOneAnswer(
        IEnumerable<VoteResultBallotQuestionAnswer> questionAnswers,
        IEnumerable<VoteResultBallotTieBreakQuestionAnswer> tieBreakQuestionAnswers)
    {
        if (questionAnswers.All(q => q.Answer == BallotQuestionAnswer.Unspecified)
            && tieBreakQuestionAnswers.All(q => q.Answer == TieBreakQuestionAnswer.Unspecified))
        {
            throw new ValidationException("At least one answer must be specified");
        }
    }
}
