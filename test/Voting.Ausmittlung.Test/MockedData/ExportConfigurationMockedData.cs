// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ExportConfigurationMockedData
{
    public const string IdStGallenIntf001 = "a14f3366-fdf5-4da1-b863-537ea41f9949";
    public const string IdStGallenIntf002 = "506caf2f-1fe2-4266-8323-ad41b215faca";
    public const string IdGossauIntf100 = "5ee34e03-83db-403e-88b7-1acd3112a49f";
    public const string IdUzwilIntf200 = "fb8ec863-34af-4429-93fe-6284be8c04c1";

    public static ExportConfiguration StGallenIntf001 => new ExportConfiguration
    {
        Id = Guid.Parse(IdStGallenIntf001),
        Description = "StGallen Interface 001",
        ExportKeys = new[]
        {
                AusmittlungXmlVoteTemplates.Ech0110.Key,
                AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources.Key,
        },
        DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
        EaiMessageType = "001",
    };

    public static ExportConfiguration StGallenIntf002 => new ExportConfiguration
    {
        Id = Guid.Parse(IdStGallenIntf002),
        Description = "StGallen Interface 002",
        ExportKeys = new[]
        {
                AusmittlungXmlVoteTemplates.Ech0222.Key,
                AusmittlungCsvProportionalElectionTemplates.CandidatesAlphabetical.Key,
        },
        DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
        EaiMessageType = "002",
    };

    public static ExportConfiguration GossauIntf100 => new ExportConfiguration
    {
        Id = Guid.Parse(IdGossauIntf100),
        Description = "Gossau Interface 100",
        ExportKeys = new[]
        {
                AusmittlungXmlVoteTemplates.Ech0222.Key,
                AusmittlungCsvProportionalElectionTemplates.CandidatesAlphabetical.Key,
        },
        DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
        EaiMessageType = "100",
    };

    public static ExportConfiguration UzwilIntf100 => new ExportConfiguration
    {
        Id = Guid.Parse(IdUzwilIntf200),
        Description = "Uzwil Interface 200",
        ExportKeys = new[]
        {
                AusmittlungXmlVoteTemplates.Ech0222.Key,
                AusmittlungCsvProportionalElectionTemplates.CandidatesAlphabetical.Key,
        },
        DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
        EaiMessageType = "200",
    };

    public static IEnumerable<ExportConfiguration> All
    {
        get
        {
            yield return StGallenIntf001;
            yield return StGallenIntf002;
            yield return GossauIntf100;
            yield return UzwilIntf100;
        }
    }

    public static Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        return runScoped(async sp =>
        {
            var all = All.ToList();
            var db = sp.GetRequiredService<DataContext>();
            db.ExportConfigurations.AddRange(all);

            var dois = db.DomainOfInfluences
                .AsSplitQuery()
                .Where(x => x.SnapshotContestId.HasValue)
                .OrderBy(x => x.Name)
                .Include(x => x.SimplePoliticalBusinesses)
                .ToList();
            var resultExportConfigs = all.SelectMany(config => dois
                .Where(x => config.DomainOfInfluenceId == x.BasisDomainOfInfluenceId)
                .Select(doi => new ResultExportConfiguration
                {
                    Id = AusmittlungUuidV5.BuildResultExportConfiguration(doi.SnapshotContestId!.Value, config.Id),
                    ContestId = doi.SnapshotContestId!.Value,
                    Description = config.Description,
                    ExportKeys = config.ExportKeys,
                    EaiMessageType = config.EaiMessageType,
                    ExportConfigurationId = config.Id,
                    DomainOfInfluenceId = doi.Id,
                    IntervalMinutes = 60,
                    PoliticalBusinesses = doi.SimplePoliticalBusinesses
                        .Select(x => new ResultExportConfigurationPoliticalBusiness
                        {
                            PoliticalBusinessId = x.Id,
                        })
                        .ToList(),
                }));

            db.ResultExportConfigurations.AddRange(resultExportConfigs);
            await db.SaveChangesAsync();
        });
    }
}
