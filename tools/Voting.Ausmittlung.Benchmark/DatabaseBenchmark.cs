// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Test.MockedData;

namespace Voting.Ausmittlung.Benchmark;

/// <summary>
/// A placeholder for performing database benchmarks.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput)]
public class DatabaseBenchmark
{
    [GlobalSetup]
    public async Task Setup()
    {
        await using var dataContext = GetDataContext();
        await dataContext.Database.EnsureDeletedAsync();
        await dataContext.Database.EnsureCreatedAsync();

        // Seed data here. Try to create roughly the same amount of data (or more) as in production
        await SeedCountingCircles(dataContext);
        await SeedDomainOfInfluences(dataContext);
        await SeedContests(dataContext);
    }

    [Benchmark]
    public void Baseline()
    {
        // Create the baseline (current implementation) here.
        // Then, add more benchmarks which should be compared to the baseline.
    }

    private static async Task SeedCountingCircles(DataContext ctx)
    {
        ctx.CountingCircles.AddRange(CountingCircleMockedData.All);
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedDomainOfInfluences(DataContext ctx)
    {
        ctx.DomainOfInfluences.AddRange(DomainOfInfluenceMockedData.All);
        ctx.DomainOfInfluenceCountingCircles.AddRange(DomainOfInfluenceMockedData.GetAllDomainOfInfluenceCountingCircles());
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedContests(DataContext ctx)
    {
        var all = ContestMockedData.All.ToList();
        ctx.Contests.AddRange(all);
        await ctx.SaveChangesAsync();
    }

    private static DataContext GetDataContext()
    {
        // Values only valid for local testing
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=voting-ausmittlung-benchmark;Username=user;Password=password");
        return new DataContext(optionsBuilder.Options);
    }
}
