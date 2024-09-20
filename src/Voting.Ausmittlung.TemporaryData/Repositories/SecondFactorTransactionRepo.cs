// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.TemporaryData.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.TemporaryData.Repositories;

public class SecondFactorTransactionRepo : DbRepository<TemporaryDataContext, SecondFactorTransaction>
{
    public SecondFactorTransactionRepo(TemporaryDataContext context)
        : base(context)
    {
    }

    public Task<SecondFactorTransaction?> GetByExternalIdentifier(string externalIdentifier)
    {
        return Set.FirstOrDefaultAsync(x => x.ExternalIdentifier == externalIdentifier);
    }
}
