// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ResultExportConfigurationWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IDbRepository<DataContext, DataModels.ResultExportConfiguration> _repo;
    private readonly PermissionService _permissionService;
    private readonly ResultExportService _resultExportService;

    public ResultExportConfigurationWriter(
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, DataModels.ResultExportConfiguration> repo,
        PermissionService permissionService,
        ResultExportService resultExportService)
    {
        _aggregateRepository = aggregateRepository;
        _repo = repo;
        _permissionService = permissionService;
        _resultExportService = resultExportService;
    }

    public async Task Update(ResultExportConfiguration config)
    {
        var id = AusmittlungUuidV5.BuildResultExportConfiguration(config.ContestId, config.ExportConfigurationId);

        await CheckAuth(id);

        await _resultExportService.ValidateExportConfigurationPoliticalBusinesses(config.ContestId, config.PoliticalBusinessIds);

        var aggregate = await _aggregateRepository.GetOrCreateById<ResultExportConfigurationAggregate>(id);
        aggregate.UpdateFrom(config, config.ContestId, config.ExportConfigurationId);
        await _aggregateRepository.Save(aggregate);
    }

    private async Task CheckAuth(Guid id)
    {
        _permissionService.EnsureMonitoringElectionAdmin();

        var config = await _repo
            .Query()
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(nameof(ResultExportConfiguration), id);
        _permissionService.EnsureIsDomainOfInfluenceManager(config.DomainOfInfluence);
    }
}
