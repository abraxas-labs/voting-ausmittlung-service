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

public class MajorityElectionResultRepo : PoliticalBusinessResultRepo<MajorityElectionResult>
{
    public MajorityElectionResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<List<MajorityElectionResult>> ListWithValidationContextData(Expression<Func<MajorityElectionResult, bool>> predicate, bool withCountingCircleData)
    {
        var query = Set
            .AsSplitQuery()
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.MajorityElection.Contest.CantonDefaults)
            .Include(x => x.MajorityElection.Contest.DomainOfInfluence)
            .Include(x => x.MajorityElection.Translations)
            .Include(x => x.CandidateResults)
            .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
            .Include(x => x.BallotGroupResults)
            .Where(predicate);

        if (withCountingCircleData)
        {
            query = query
                .Include(x => x.CountingCircle.ResponsibleAuthority);
        }

        return query.ToListAsync();
    }

    protected override Expression<Func<MajorityElectionResult, bool>> FilterByPoliticalBusinessId(Guid id) =>
        x => x.MajorityElectionId == id;

    protected override async Task<PoliticalBusiness> LoadPoliticalBusiness(Guid id)
    {
        return await Context.Set<MajorityElection>().Where(x => x.Id == id).SingleAsync();
    }
}
