// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class VoteResultRepo : PoliticalBusinessResultRepo<VoteResult>
{
    public VoteResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<VoteResult?> GetVoteResultWithQuestionResultsAsTracked(Guid id)
    {
        return GetVoteResultQueryWithQuestionResultsAsTracked()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public Task<List<VoteResult>> GetVoteResultsWithQuestionResultsAsTracked(Guid contestId, Guid countingCircleId)
    {
        return GetVoteResultQueryWithQuestionResultsAsTracked()
            .Where(x => x.CountingCircleId == countingCircleId && x.Vote.ContestId == contestId)
            .ToListAsync();
    }

    public Task<VoteResult?> GetVoteResultWithRelations(Guid id)
    {
        return Set
            .AsSplitQuery()
            .Include(b => b.Results).ThenInclude(r => r.QuestionResults)
            .Include(b => b.Results).ThenInclude(r => r.TieBreakQuestionResults)
            .Include(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.VotingCards)
            .Include(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public Task<List<VoteResult>> ListWithValidationContextData(Expression<Func<VoteResult, bool>> predicate, bool withCountingCircleAndContestData)
    {
        var query = Set
            .AsSplitQuery()
            .Include(x => x.Vote.DomainOfInfluence)
            .Include(x => x.Vote.Translations)
            .Include(x => x.Results).ThenInclude(x => x.Ballot)
            .Include(x => x.Results).ThenInclude(x => x.QuestionResults).ThenInclude(x => x.Question)
            .Include(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults).ThenInclude(x => x.Question)
            .Where(predicate);

        if (withCountingCircleAndContestData)
        {
            query = query
                .Include(x => x.Vote.Contest.DomainOfInfluence)
                .Include(x => x.CountingCircle.ResponsibleAuthority);
        }

        return query.ToListAsync();
    }

    protected override Expression<Func<VoteResult, bool>> FilterByPoliticalBusinessId(Guid id) =>
        vr => vr.VoteId == id;

    protected override async Task<PoliticalBusiness> LoadPoliticalBusiness(Guid id)
    {
        return await Context.Set<Vote>().Include(x => x.DomainOfInfluence).Where(x => x.Id == id).SingleAsync();
    }

    private IQueryable<VoteResult> GetVoteResultQueryWithQuestionResultsAsTracked()
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(b => b.Vote)
            .Include(b => b.Results).ThenInclude(r => r.Ballot)
            .Include(b => b.Results).ThenInclude(r => r.QuestionResults).ThenInclude(qr => qr.Question)
            .Include(b => b.Results).ThenInclude(r => r.TieBreakQuestionResults).ThenInclude(r => r.Question)
            .Include(b => b.Results).ThenInclude(r => r.Bundles);
    }
}
