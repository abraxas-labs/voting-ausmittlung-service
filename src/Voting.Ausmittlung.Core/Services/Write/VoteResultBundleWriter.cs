// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class VoteResultBundleWriter
    : PoliticalBusinessResultBundleWriter<DataModels.VoteResult, VoteResultBundleAggregate>
{
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IDbRepository<DataContext, DataModels.VoteResult> _resultRepo;
    private readonly IDbRepository<DataContext, DataModels.VoteResultBundle> _bundleRepo;
    private readonly IDbRepository<DataContext, DataModels.BallotResult> _ballotResultRepo;

    public VoteResultBundleWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        IDbRepository<DataContext, DataModels.VoteResult> resultRepo,
        IDbRepository<DataContext, DataModels.BallotResult> ballotResultRepo,
        IDbRepository<DataContext, DataModels.VoteResultBundle> bundleRepo,
        PermissionService permissionService,
        ContestService contestService,
        IAuth auth)
        : base(permissionService, contestService, auth, aggregateRepository)
    {
        _aggregateFactory = aggregateFactory;
        _resultRepo = resultRepo;
        _ballotResultRepo = ballotResultRepo;
        _bundleRepo = bundleRepo;
    }

    public async Task<VoteResultBundleAggregate> CreateBundle(
        Guid voteResultId,
        Guid ballotResultId,
        int? bundleNumber)
    {
        var contestId = await EnsurePoliticalBusinessPermissions(voteResultId);

        var voteResultAggregate = await AggregateRepository.GetById<VoteResultAggregate>(voteResultId);

        if (voteResultAggregate.ResultEntryParams.AutomaticBallotBundleNumberGeneration)
        {
            bundleNumber = voteResultAggregate.GenerateBundleNumber(ballotResultId, contestId);
        }
        else if (bundleNumber.HasValue)
        {
            voteResultAggregate.BundleNumberEntered(bundleNumber.Value, ballotResultId, contestId);
        }
        else
        {
            throw new ValidationException("bundle number is not generated automatically and should be provided");
        }

        var aggregate = _aggregateFactory.New<VoteResultBundleAggregate>();
        aggregate.Create(
            null,
            voteResultId,
            ballotResultId,
            bundleNumber.Value,
            voteResultAggregate.ResultEntryParams,
            contestId);

        await AggregateRepository.Save(voteResultAggregate);
        await AggregateRepository.Save(aggregate);
        return aggregate;
    }

    public async Task DeleteBundle(Guid bundleId, Guid ballotResultId)
    {
        var aggregate = await AggregateRepository.GetById<VoteResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.Delete(contestId);

        var voteResultAggregate = await AggregateRepository.GetById<VoteResultAggregate>(aggregate.PoliticalBusinessResultId);
        voteResultAggregate.FreeBundleNumber(aggregate.BundleNumber, ballotResultId, contestId);

        await AggregateRepository.Save(voteResultAggregate);
        await AggregateRepository.Save(aggregate);
    }

    public async Task<int> CreateBallot(
        Guid bundleId,
        ICollection<VoteResultBallotQuestionAnswer> questionAnswers,
        ICollection<VoteResultBallotTieBreakQuestionAnswer> tieBreakQuestionAnswers)
    {
        var aggregate = await AggregateRepository.GetById<VoteResultBundleAggregate>(bundleId);
        var result = await LoadPoliticalBusinessResult(aggregate.PoliticalBusinessResultId);

        var contestId = await EnsureEditPermissionsForBundle(result, aggregate);

        var questionBallotNumbers = questionAnswers.Select(x => x.QuestionNumber).ToList();
        var tieBreakQuestionBallotNumbers = tieBreakQuestionAnswers.Select(x => x.QuestionNumber).ToList();
        var ballotResult = await LoadBallotResult(aggregate.BallotResultId);

        EnsureValidQuestions(ballotResult.Ballot.BallotQuestions.Select(b => b.Number), questionBallotNumbers);
        EnsureValidQuestions(ballotResult.Ballot.TieBreakQuestions.Select(b => b.Number), tieBreakQuestionBallotNumbers);

        aggregate.CreateBallot(questionAnswers, tieBreakQuestionAnswers, contestId);
        await AggregateRepository.Save(aggregate);
        return aggregate.CurrentBallotNumber;
    }

    public async Task UpdateBallot(
        Guid bundleId,
        int ballotNumber,
        ICollection<VoteResultBallotQuestionAnswer> questionAnswers,
        ICollection<VoteResultBallotTieBreakQuestionAnswer> tieBreakQuestionAnswers)
    {
        var aggregate = await AggregateRepository.GetById<VoteResultBundleAggregate>(bundleId);
        var result = await LoadPoliticalBusinessResult(aggregate.PoliticalBusinessResultId);

        var contestId = await EnsureEditPermissionsForBundle(result, aggregate);

        var questionBallotNumbers = questionAnswers.Select(x => x.QuestionNumber).ToList();
        var tieBreakQuestionBallotNumbers = tieBreakQuestionAnswers.Select(x => x.QuestionNumber).ToList();
        var ballotResult = await LoadBallotResult(aggregate.BallotResultId);

        EnsureValidQuestions(ballotResult.Ballot.BallotQuestions.Select(b => b.Number), questionBallotNumbers);
        EnsureValidQuestions(ballotResult.Ballot.TieBreakQuestions.Select(b => b.Number), tieBreakQuestionBallotNumbers);

        aggregate.UpdateBallot(ballotNumber, questionAnswers, tieBreakQuestionAnswers, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task DeleteBallot(Guid bundleId, int ballotNumber)
    {
        var aggregate = await AggregateRepository.GetById<VoteResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.DeleteBallot(ballotNumber, contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task BundleSubmissionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<VoteResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.SubmissionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task BundleCorrectionFinished(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<VoteResultBundleAggregate>(bundleId);
        var contestId = await EnsureEditPermissionsForBundle(aggregate);
        aggregate.CorrectionFinished(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task RejectBundleReview(Guid bundleId)
    {
        var aggregate = await AggregateRepository.GetById<VoteResultBundleAggregate>(bundleId);
        var contestId = await EnsureReviewPermissionsForBundle(aggregate);
        aggregate.RejectReview(contestId);
        await AggregateRepository.Save(aggregate);
    }

    public async Task SucceedBundleReview(IReadOnlyCollection<Guid> bundleIds)
    {
        await ExecuteOnAllAggregates<VoteResultBundleAggregate>(bundleIds, async aggregate =>
        {
            var contestId = await EnsureReviewPermissionsForBundle(aggregate);
            aggregate.SucceedReview(contestId);
        });
    }

    protected override async Task<DataModels.VoteResult> LoadPoliticalBusinessResult(Guid resultId)
    {
        return await _resultRepo.Query()
                   .Include(vr => vr.Vote.Contest)
                   .Include(vr => vr.Vote.DomainOfInfluence)
                   .FirstOrDefaultAsync(x => x.Id == resultId)
               ?? throw new EntityNotFoundException(resultId);
    }

    protected override Task<bool> DoesBundleExist(Guid id)
        => _bundleRepo.ExistsByKey(id);

    private async Task<DataModels.BallotResult> LoadBallotResult(Guid ballotResultId)
    {
        return await _ballotResultRepo.Query()
                   .AsSplitQuery()
                   .Include(x => x.Ballot.BallotQuestions)
                   .Include(x => x.Ballot.TieBreakQuestions)
                   .FirstOrDefaultAsync(x => x.Id == ballotResultId)
                   ?? throw new EntityNotFoundException(ballotResultId);
    }

    private void EnsureValidQuestions(IEnumerable<int> questionNumbers, IReadOnlyCollection<int> suppliedQuestionNumbers)
    {
        if (suppliedQuestionNumbers.ToHashSet().Count != suppliedQuestionNumbers.Count)
        {
            throw new ValidationException("duplicated questions provided");
        }

        if (suppliedQuestionNumbers.Except(questionNumbers).Any())
        {
            throw new ValidationException("unknown questions provided");
        }
    }
}
