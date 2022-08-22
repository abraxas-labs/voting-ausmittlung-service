// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class CantonSettingsRepo : DbRepository<DataContext, CantonSettings>
{
    public CantonSettingsRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<CantonSettings?> GetByDomainOfInfluenceCanton(DomainOfInfluenceCanton canton)
    {
        return await Query()
            .Include(x => x.EnabledVotingCardChannels)
            .SingleOrDefaultAsync(x => x.Canton == canton);
    }
}
