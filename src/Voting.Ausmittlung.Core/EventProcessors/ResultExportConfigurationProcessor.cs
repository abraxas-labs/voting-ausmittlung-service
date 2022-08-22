// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ResultExportConfigurationProcessor : IEventProcessor<ResultExportConfigurationUpdated>
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
                                 .Include(x => x.PoliticalBusinesses)
                                 .FirstOrDefaultAsync(x => x.ContestId == contestId && x.ExportConfigurationId == configId)
                             ?? throw new EntityNotFoundException(nameof(ResultExportConfiguration), new { contestId, configId });

        SyncPoliticalBusinesses(existingConfig.PoliticalBusinesses!, eventData.ExportConfiguration.PoliticalBusinessIds);

        existingConfig.IntervalMinutes = eventData.ExportConfiguration.IntervalMinutes;
        existingConfig.UpdateNextExecution(_clock.UtcNow);

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
}
