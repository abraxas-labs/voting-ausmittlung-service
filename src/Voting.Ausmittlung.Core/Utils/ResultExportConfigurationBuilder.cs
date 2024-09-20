// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ResultExportConfigurationBuilder
{
    private readonly IDbRepository<DataContext, ExportConfiguration> _exportConfigRepo;
    private readonly ResultExportConfigurationRepo _resultExportConfigRepo;
    private readonly DomainOfInfluenceRepo _domainOfInfluenceRepo;

    public ResultExportConfigurationBuilder(
        IDbRepository<DataContext, ExportConfiguration> exportConfigRepo,
        ResultExportConfigurationRepo resultExportConfigRepo,
        DomainOfInfluenceRepo domainOfInfluenceRepo)
    {
        _exportConfigRepo = exportConfigRepo;
        _resultExportConfigRepo = resultExportConfigRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
    }

    internal async Task CreateResultExportConfigurationForContest(Contest contest)
    {
        var dois = await _domainOfInfluenceRepo
            .Query()
            .Where(x => x.SnapshotContestId == contest.Id)
            .Select(x => new
            {
                x.Id,
                x.BasisDomainOfInfluenceId,
            })
            .ToListAsync();

        var configs = await _exportConfigRepo
            .Query()
            .Where(x => dois.Select(doi => doi.BasisDomainOfInfluenceId).Contains(x.DomainOfInfluenceId))
            .ToListAsync();

        var configsByDoiId = configs
            .GroupBy(x => x.DomainOfInfluenceId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var resultExportConfigs = dois.SelectMany(doi =>
        {
            if (!configsByDoiId.TryGetValue(doi.BasisDomainOfInfluenceId, out var doiConfigs))
            {
                return Enumerable.Empty<ResultExportConfiguration>();
            }

            return doiConfigs.Select(config => new ResultExportConfiguration
            {
                Id = AusmittlungUuidV5.BuildResultExportConfiguration(contest.Id, config.Id),
                ContestId = contest.Id,
                Description = config.Description,
                ExportKeys = config.ExportKeys,
                EaiMessageType = config.EaiMessageType,
                ExportConfigurationId = config.Id,
                Provider = config.Provider,
                DomainOfInfluenceId = doi.Id,
            });
        });

        await _resultExportConfigRepo.CreateRange(resultExportConfigs);
    }
}
