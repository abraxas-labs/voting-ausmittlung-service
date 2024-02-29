// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionResultRepo : PoliticalBusinessResultRepo<ProportionalElectionResult>
{
    public ProportionalElectionResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<List<ProportionalElectionResult>> ListWithValidationContextData(Expression<Func<ProportionalElectionResult, bool>> predicate, bool withCountingCircleAndContestData)
    {
        var query = Set.AsSplitQuery()
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.ProportionalElection.Translations)
            .Where(predicate);

        if (withCountingCircleAndContestData)
        {
            query = query
                .Include(x => x.ProportionalElection.Contest.DomainOfInfluence)
                .Include(x => x.CountingCircle.ResponsibleAuthority);
        }

        return query.ToListAsync();
    }

    protected override Expression<Func<ProportionalElectionResult, bool>> FilterByPoliticalBusinessId(Guid id) =>
        x => x.ProportionalElectionId == id;
}
