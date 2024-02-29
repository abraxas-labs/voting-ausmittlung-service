// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Voting.Ausmittlung.TemporaryData;

internal class TemporaryDataContextFactory : IDesignTimeDbContextFactory<TemporaryDataContext>
{
    public TemporaryDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TemporaryDataContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=db;Username=user;Password=pass");

        return new TemporaryDataContext(optionsBuilder.Options);
    }
}
