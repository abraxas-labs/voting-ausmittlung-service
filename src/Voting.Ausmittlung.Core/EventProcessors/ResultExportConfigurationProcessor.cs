// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ResultExportConfigurationProcessor : IEventProcessor<ResultExportConfigurationUpdated>,
    IEventProcessor<ResultExportCompleted>
{
    private readonly DataContext _dbContext;
    private readonly IClock _clock;

    public ResultExportConfigurationProcessor(IClock clock, DataContext dbContext)
    {
        _clock = clock;
        _dbContext = dbContext;
    }

    public async Task Process(ResultExportConfigurationUpdated eventData)
    {
        var contestId = GuidParser.Parse(eventData.ExportConfiguration.ContestId);
        var configId = GuidParser.Parse(eventData.ExportConfiguration.ExportConfigurationId);
        var existingConfig = await _dbContext.ResultExportConfigurations
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.PoliticalBusinesses)
            .Include(x => x.PoliticalBusinessMetadata)
            .FirstOrDefaultAsync(x => x.ContestId == contestId && x.ExportConfigurationId == configId)
            ?? throw new EntityNotFoundException(nameof(ResultExportConfiguration), new { contestId, configId });

        SyncPoliticalBusinesses(existingConfig.PoliticalBusinesses!, eventData.ExportConfiguration.PoliticalBusinessIds);
        SyncPoliticalBusinessesMetadata(existingConfig.PoliticalBusinessMetadata!, eventData.ExportConfiguration.PoliticalBusinessMetadata);

        existingConfig.IntervalMinutes = eventData.ExportConfiguration.IntervalMinutes;
        existingConfig.UpdateNextExecution(_clock.UtcNow);

        await _dbContext.SaveChangesAsync();
    }

    public async Task Process(ResultExportCompleted eventData)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);
        var configId = GuidParser.Parse(eventData.ExportConfigurationId);
        var existingConfig = await _dbContext.ResultExportConfigurations
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ContestId == contestId && x.ExportConfigurationId == configId)
            ?? throw new EntityNotFoundException(nameof(ResultExportConfiguration), new { contestId, configId });

        existingConfig.LatestExecution = _clock.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Synchronizes the list of political businesses with a list of political business IDs.
    /// Political businesses not present <paramref name="newIds"/> will be removed.
    /// Political businesses present in <paramref name="newIds"/> will be added to the political business list if not already present.
    /// </summary>
    /// <param name="politicalBusinesses">The existing list of political businesses which will be modified.</param>
    /// <param name="newIds">The list of new political business IDs.</param>
    private void SyncPoliticalBusinesses(
        ICollection<ResultExportConfigurationPoliticalBusiness> politicalBusinesses,
        IEnumerable<string> newIds)
    {
        var existingPoliticalBusinesses = politicalBusinesses.ToDictionary(x => x.PoliticalBusinessId);

        foreach (var pbIdStr in newIds.Distinct())
        {
            var pbId = GuidParser.Parse(pbIdStr);
            if (!existingPoliticalBusinesses.Remove(pbId))
            {
                politicalBusinesses.Add(new ResultExportConfigurationPoliticalBusiness
                {
                    PoliticalBusinessId = pbId,
                });
            }
        }

        foreach (var existingPoliticalBusiness in existingPoliticalBusinesses.Values)
        {
            politicalBusinesses.Remove(existingPoliticalBusiness);
        }
    }

    /// <summary>
    /// Synchronizes the list of political business metadata with a map of new metadata.
    /// Metadata not present <paramref name="newMetadata"/> will be removed.
    /// Metadata present in <paramref name="newMetadata"/> and <paramref name="politicalBusinessesMetadata"/> will be updated.
    /// Metadata present in <paramref name="newMetadata"/> but not <paramref name="politicalBusinessesMetadata"/> will be added.
    /// </summary>
    /// <param name="politicalBusinessesMetadata">The existing list of political business metadata which will be modified.</param>
    /// <param name="newMetadata">The new political business metadata map.</param>
    private void SyncPoliticalBusinessesMetadata(
        ICollection<ResultExportConfigurationPoliticalBusinessMetadata> politicalBusinessesMetadata,
        IDictionary<string, PoliticalBusinessExportMetadataEventData> newMetadata)
    {
        var existingMetadatas = politicalBusinessesMetadata.ToDictionary(x => x.PoliticalBusinessId);

        foreach (var (pbIdStr, metadata) in newMetadata)
        {
            var pbId = GuidParser.Parse(pbIdStr);
            if (existingMetadatas.Remove(pbId, out var existingMetadata))
            {
                existingMetadata.Token = metadata.Token;
            }
            else
            {
                politicalBusinessesMetadata.Add(new ResultExportConfigurationPoliticalBusinessMetadata
                {
                    PoliticalBusinessId = pbId,
                    Token = metadata.Token,
                });
            }
        }

        foreach (var existingMetadata in existingMetadatas.Values)
        {
            politicalBusinessesMetadata.Remove(existingMetadata);
        }
    }
}
