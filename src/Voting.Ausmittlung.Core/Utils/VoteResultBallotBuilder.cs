// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class VoteResultBallotBuilder
{
    private readonly IDbRepository<DataContext, VoteResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, VoteResultBundle> _bundleRepo;
    private readonly DataContext _dbContext;
    private readonly IMapper _mapper;

    public VoteResultBallotBuilder(
        IDbRepository<DataContext, VoteResultBallot> ballotRepo,
        IDbRepository<DataContext, VoteResultBundle> bundleRepo,
        DataContext dbContext,
        IMapper mapper)
    {
        _ballotRepo = ballotRepo;
        _bundleRepo = bundleRepo;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    internal async Task CreateBallot(
        Guid bundleId,
        VoteResultBallotCreated data)
    {
        var bundle = await _bundleRepo.Query()
            .AsSplitQuery()
            .Where(x => x.Id == bundleId)
            .Select(x => new
            {
                QuestionInfos = x.BallotResult.Ballot.BallotQuestions.Select(q => new
                {
                    q.Id,
                    q.Number,
                }).ToList(),
                TieBreakQuestionInfos = x.BallotResult.Ballot.TieBreakQuestions.Select(q => new
                {
                    q.Id,
                    q.Number,
                }).ToList(),
            })
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(bundleId);

        var ballot = new VoteResultBallot
        {
            Number = data.BallotNumber,
            Index = data.Index ?? data.BallotNumber,
            BundleId = bundleId,
        };

        ReplaceBallotQuestionAnswers(ballot, data.QuestionAnswers, bundle.QuestionInfos.ToDictionary(x => x.Number, x => x.Id));
        ReplaceBallotTieBreakQuestionAnswers(ballot, data.TieBreakQuestionAnswers, bundle.TieBreakQuestionInfos.ToDictionary(x => x.Number, x => x.Id));
        await _ballotRepo.Create(ballot);
    }

    internal async Task UpdateBallot(
        Guid bundleId,
        VoteResultBallotUpdated data)
    {
        var ballot = await _ballotRepo
            .Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.QuestionAnswers)
            .Include(x => x.TieBreakQuestionAnswers)
            .Include(x => x.Bundle).ThenInclude(x => x.BallotResult).ThenInclude(x => x.Ballot).ThenInclude(x => x.BallotQuestions)
            .Include(x => x.Bundle).ThenInclude(x => x.BallotResult).ThenInclude(x => x.Ballot).ThenInclude(x => x.TieBreakQuestions)
            .FirstOrDefaultAsync(x => x.Number == data.BallotNumber && x.BundleId == bundleId)
            ?? throw new EntityNotFoundException(new { bundleId, data.BallotNumber });

        ReplaceBallotQuestionAnswers(ballot, data.QuestionAnswers, ballot.Bundle.BallotResult.Ballot.BallotQuestions.ToDictionary(x => x.Number, x => x.Id));
        ReplaceBallotTieBreakQuestionAnswers(ballot, data.TieBreakQuestionAnswers, ballot.Bundle.BallotResult.Ballot.TieBreakQuestions.ToDictionary(x => x.Number, x => x.Id));

        if (ballot.Bundle.State > BallotBundleState.InProcess)
        {
            ballot.Logs.Add(new VoteResultBallotLog
            {
                User = data.EventInfo.User.ToDataUser(),
                Timestamp = data.EventInfo.Timestamp.ToDateTime(),
            });
        }

        if (ballot.Bundle.State == BallotBundleState.ReadyForReview)
        {
            ballot.ModifiedDuringReview = true;
        }

        await _dbContext.SaveChangesAsync();
    }

    private void ReplaceBallotQuestionAnswers(
        VoteResultBallot ballot,
        IEnumerable<VoteResultBallotUpdatedQuestionAnswerEventData> answers,
        Dictionary<int, Guid> questionsByNumber)
    {
        ballot.QuestionAnswers.Clear();

        foreach (var answer in answers)
        {
            if (!questionsByNumber.TryGetValue(answer.QuestionNumber, out var questionId))
            {
                throw new ValidationException("unknown question number is not allowed");
            }

            ballot.QuestionAnswers.Add(new VoteResultBallotQuestionAnswer
            {
                QuestionId = questionId,
                Answer = _mapper.Map<BallotQuestionAnswer>(answer.Answer),
            });
        }
    }

    private void ReplaceBallotTieBreakQuestionAnswers(
        VoteResultBallot ballot,
        IEnumerable<VoteResultBallotUpdatedTieBreakQuestionAnswerEventData> answers,
        Dictionary<int, Guid> questionsByNumber)
    {
        ballot.TieBreakQuestionAnswers.Clear();

        foreach (var answer in answers)
        {
            if (!questionsByNumber.TryGetValue(answer.QuestionNumber, out var questionId))
            {
                throw new ValidationException("unknown question number is not allowed");
            }

            ballot.TieBreakQuestionAnswers.Add(new VoteResultBallotTieBreakQuestionAnswer
            {
                QuestionId = questionId,
                Answer = _mapper.Map<TieBreakQuestionAnswer>(answer.Answer),
            });
        }
    }
}
