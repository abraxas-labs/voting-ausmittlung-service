// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.TemporaryData.Models;

namespace Voting.Ausmittlung.TemporaryData;

public class TemporaryDataContext : DbContext
{
    public TemporaryDataContext(DbContextOptions<TemporaryDataContext> options)
        : base(options)
    {
    }

    // nullables see https://docs.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types
    public DbSet<SecondFactorTransaction> SecondFactorTransactions { get; set; } = null!;
}
