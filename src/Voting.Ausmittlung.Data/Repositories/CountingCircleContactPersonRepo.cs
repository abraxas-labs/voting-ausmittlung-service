// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class CountingCircleContactPersonRepo : DbRepository<DataContext, CountingCircleContactPerson>
{
    public CountingCircleContactPersonRepo(DataContext context)
        : base(context)
    {
    }
}
