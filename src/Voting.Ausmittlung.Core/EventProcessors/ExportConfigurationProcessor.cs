// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ExportConfigurationProcessor :
        IEventProcessor<ExportConfigurationCreated>,
        IEventProcessor<ExportConfigurationUpdated>,
        IEventProcessor<ExportConfigurationDeleted>
{
    private readonly IDbRepository<DataContext, ExportConfiguration> _exportConfigRepo;
    private readonly ResultExportConfigurationRepo _resultExportConfigRepo;
    private readonly DomainOfInfluenceRepo _domainOfInfluenceRepo;
    private readonly IMapper _mapper;

    public ExportConfigurationProcessor(
        IDbRepository<DataContext, ExportConfiguration> exportConfigRepo,
        ResultExportConfigurationRepo resultExportConfigRepo,
        DomainOfInfluenceRepo domainOfInfluenceRepo,
        IMapper mapper)
    {
        _exportConfigRepo = exportConfigRepo;
        _resultExportConfigRepo = resultExportConfigRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
        _mapper = mapper;
    }

    public async Task Process(ExportConfigurationCreated eventData)
    {
        AdjustOldEvents(eventData.Configuration);
        var config = _mapper.Map<ExportConfiguration>(eventData.Configuration);
        await _exportConfigRepo.Create(config);

        var dois = await _domainOfInfluenceRepo.ListWithContestsInState(
            config.DomainOfInfluenceId,
            ContestState.Active,
            ContestState.TestingPhase);

        var resultExportConfigs = dois.Select(doi => new ResultExportConfiguration
        {
            Id = AusmittlungUuidV5.BuildResultExportConfiguration(doi.SnapshotContestId!.Value, config.Id),
            ContestId = doi.SnapshotContestId!.Value,
            Description = config.Description,
            ExportKeys = config.ExportKeys,
            EaiMessageType = config.EaiMessageType,
            ExportConfigurationId = config.Id,
            Provider = config.Provider,
            DomainOfInfluenceId = doi.Id,
        });

        await _resultExportConfigRepo.CreateRange(resultExportConfigs);
    }

    public async Task Process(ExportConfigurationUpdated eventData)
    {
        AdjustOldEvents(eventData.Configuration);
        var config = _mapper.Map<ExportConfiguration>(eventData.Configuration);
        await _exportConfigRepo.Update(config);

        var existingResultConfigs = await _resultExportConfigRepo.GetActiveOrTestingPhaseExportConfigurations(config.Id);
        foreach (var resultConfig in existingResultConfigs)
        {
            resultConfig.Description = config.Description;
            resultConfig.ExportKeys = config.ExportKeys;
            resultConfig.EaiMessageType = config.EaiMessageType;
            resultConfig.Provider = config.Provider;
        }

        await _resultExportConfigRepo.UpdateRange(existingResultConfigs);
    }

    public async Task Process(ExportConfigurationDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ConfigurationId);
        await _exportConfigRepo.DeleteByKey(id);

        var existingResultConfigs = await _resultExportConfigRepo.GetActiveOrTestingPhaseExportConfigurations(id);
        await _resultExportConfigRepo.DeleteRangeByKey(existingResultConfigs.Select(x => x.Id));
    }

    private void AdjustOldEvents(ExportConfigurationEventData eventData)
    {
        // "Old" events that were created before the export provider was implemented need to be adjusted
        if (eventData.Provider == Abraxas.Voting.Basis.Shared.V1.ExportProvider.Unspecified)
        {
            eventData.Provider = Abraxas.Voting.Basis.Shared.V1.ExportProvider.Standard;
        }
    }
}
