// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class DoubleProportionalResultRepo : DbRepository<DataContext, DoubleProportionalResult>
{
    public DoubleProportionalResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<DoubleProportionalResult?> GetUnionDoubleProportionalResultAsTracking(Guid proportionalElectionUnionId, string? secureConnectId = null)
    {
        return UnionDoubleProportionalResultQuery(proportionalElectionUnionId, secureConnectId)
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public Task<DoubleProportionalResult?> GetUnionDoubleProportionalResult(Guid proportionalElectionUnionId, string? secureConnectId = null)
    {
        return UnionDoubleProportionalResultQuery(proportionalElectionUnionId, secureConnectId)
            .FirstOrDefaultAsync();
    }

    public Task<DoubleProportionalResult?> GetElectionDoubleProportionalResultAsTracking(Guid proportionalElectionId, string? secureConnectId = null)
    {
        return ElectionDoubleProportionalResultQuery(proportionalElectionId, secureConnectId)
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public Task<DoubleProportionalResult?> GetElectionDoubleProportionalResult(Guid proportionalElectionId, string? secureConnectId = null)
    {
        return ElectionDoubleProportionalResultQuery(proportionalElectionId, secureConnectId)
            .FirstOrDefaultAsync();
    }

    private IQueryable<DoubleProportionalResult> UnionDoubleProportionalResultQuery(Guid proportionalElectionUnionId, string? secureConnectId = null)
    {
        return Query()
            .AsSplitQuery()
            .Include(dp => dp.ProportionalElectionUnion!.Contest.Translations)
            .Include(dp => dp.ProportionalElectionUnion!.Contest.CantonDefaults)
            .Include(dp => dp.Rows.OrderBy(e => e.ProportionalElection.PoliticalBusinessNumber))
            .ThenInclude(r => r.ProportionalElection.Translations)
            .Include(dp => dp.Rows)
            .ThenInclude(r => r.ProportionalElection.DomainOfInfluence)
            .Include(dp => dp.Rows)
            .ThenInclude(r => r.Cells.OrderBy(l => l.List.OrderNumber))
            .ThenInclude(ce => ce.List.Translations)
            .Include(dp => dp.Columns.OrderBy(co => co.UnionList!.OrderNumber))
            .ThenInclude(ul => ul.Cells.OrderBy(ce => ce.List.ProportionalElection.PoliticalBusinessNumber))
            .ThenInclude(ce => ce.List.Translations)
            .Include(dp => dp.Columns)
            .ThenInclude(co => co.UnionList!.Translations)
            .Where(dp => dp.ProportionalElectionUnionId == proportionalElectionUnionId && (secureConnectId == null || dp.ProportionalElectionUnion!.SecureConnectId == secureConnectId));
    }

    private IQueryable<DoubleProportionalResult> ElectionDoubleProportionalResultQuery(Guid proportionalElectionId, string? secureConnectId = null)
    {
        return Query()
            .AsSplitQuery()
            .Include(dp => dp.ProportionalElection!.Contest.Translations)
            .Include(dp => dp.ProportionalElection!.Contest.CantonDefaults)
            .Include(dp => dp.ProportionalElection!.Translations)
            .Include(dp => dp.Rows.OrderBy(r => r.ProportionalElection.PoliticalBusinessNumber))
            .ThenInclude(r => r.ProportionalElection.Translations)
            .Include(dp => dp.Rows)
            .ThenInclude(r => r.ProportionalElection.DomainOfInfluence)
            .Include(dp => dp.Rows)
            .ThenInclude(r => r.Cells.OrderBy(ce => ce.List.OrderNumber))
            .ThenInclude(ce => ce.List.Translations)
            .Include(dp => dp.Columns.OrderBy(co => co.List!.OrderNumber))
            .ThenInclude(co => co.Cells)
            .ThenInclude(ce => ce.List.Translations)
            .Include(dp => dp.Columns)
            .ThenInclude(co => co.List!.Translations)
            .Where(dp => dp.ProportionalElectionId == proportionalElectionId && (secureConnectId == null || dp.ProportionalElection!.DomainOfInfluence.SecureConnectId == secureConnectId));
    }
}
