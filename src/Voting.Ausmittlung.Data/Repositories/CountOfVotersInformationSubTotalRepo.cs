// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class CountOfVotersInformationSubTotalRepo : DbRepository<DataContext, CountOfVotersInformationSubTotal>
{
    public CountOfVotersInformationSubTotalRepo(DataContext context)
        : base(context)
    {
    }

    public Task<Dictionary<(Guid CountingCircleId, DomainOfInfluenceType DomainOfInfluenceType), int?>> GetCountOfVotersByCountCircleId(Guid contestId, VoterType voterType, CancellationToken ct = default)
    {
        return Query()
            .Where(x => x.VoterType == voterType && x.ContestCountingCircleDetails.ContestId == contestId)
            .GroupBy(x => new { x.ContestCountingCircleDetails.CountingCircleId, x.DomainOfInfluenceType })
            .Select(x => new
            {
                x.Key.CountingCircleId,
                x.Key.DomainOfInfluenceType,
                Sum = x.Sum(z => z.CountOfVoters),
            })
            .ToDictionaryAsync(x => (x.CountingCircleId, x.DomainOfInfluenceType), x => x.Sum, ct);
    }
}
