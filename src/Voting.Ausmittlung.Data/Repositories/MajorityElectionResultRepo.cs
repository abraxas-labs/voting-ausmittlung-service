// (c) Copyright 2022 by Abraxas Informatik AG
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

    public Task<List<MajorityElectionResult>> ListWithValidationContextData(Expression<Func<MajorityElectionResult, bool>> predicate, bool withCountingCircleAndContestData)
    {
        var query = Set
            .AsSplitQuery()
            .Include(x => x.MajorityElection.DomainOfInfluence.CantonDefaults)
            .Include(x => x.MajorityElection.Translations)
            .Include(x => x.CandidateResults)
            .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
            .Include(x => x.BallotGroupResults)
            .Where(predicate);

        if (withCountingCircleAndContestData)
        {
            query = query
                .Include(x => x.MajorityElection.Contest.DomainOfInfluence)
                .Include(x => x.CountingCircle.ResponsibleAuthority);
        }

        return query.ToListAsync();
    }

    protected override Expression<Func<MajorityElectionResult, bool>> FilterByPoliticalBusinessId(Guid id) =>
        x => x.MajorityElectionId == id;
}
