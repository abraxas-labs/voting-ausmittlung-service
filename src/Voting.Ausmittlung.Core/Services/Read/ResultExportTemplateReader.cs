// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Resources;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;
using Voting.Lib.Messaging;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ResultExportTemplateReader
{
    private static readonly IReadOnlySet<string> _templateKeysOnlyWithInvalidVotes = new HashSet<string>
    {
        AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key,
    };

    private static readonly IReadOnlySet<string> _templateKeysCountingCircleEVoting = new HashSet<string>
    {
        AusmittlungPdfMajorityElectionTemplates.CountingCircleEVotingProtocol.Key,
        AusmittlungPdfSecondaryMajorityElectionTemplates.CountingCircleEVotingProtocol.Key,
        AusmittlungPdfProportionalElectionTemplates.ListVotesCountingCircleEVotingProtocol.Key,
        AusmittlungPdfProportionalElectionTemplates.ListCandidateEmptyVotesCountingCircleEVotingProtocol.Key,
        AusmittlungPdfVoteTemplates.EVotingCountingCircleResultProtocol.Key,
    };

    private static readonly IReadOnlySet<string> _templateKeysPoliticalBusinessEVoting = new HashSet<string>
    {
        AusmittlungPdfMajorityElectionTemplates.EndResultEVotingProtocol.Key,
        AusmittlungPdfSecondaryMajorityElectionTemplates.EndResultEVotingProtocol.Key,
        AusmittlungPdfProportionalElectionTemplates.EndResultListUnionsEVoting.Key,
        AusmittlungPdfProportionalElectionTemplates.ListCandidateEndResultsEVoting.Key,
        AusmittlungPdfVoteTemplates.EVotingDetailsResultProtocol.Key,
        AusmittlungPdfVoteTemplates.EVotingResultProtocol.Key,
        AusmittlungCsvVoteTemplates.EVotingDetails.Key,
    };

    private static readonly IReadOnlySet<string> _templateKeysProportionalElectionMandateAlgorithmUnionDoubleProportional = new HashSet<string>
    {
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultQuorumUnionListDoubleProportional.Key,
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultSubApportionmentDoubleProportional.Key,
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultSuperApportionmentDoubleProportional.Key,
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultNumberOfMandatesDoubleProportional.Key,
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultCalculationDoubleProportional.Key,
    };

    private static readonly IReadOnlySet<string> _templateKeysProportionalElectionMandateAlgorithmHagenbachBischoff = new HashSet<string>
    {
        AusmittlungPdfProportionalElectionTemplates.ListVotesPoliticalBusinessUnionEndResults.Key,
    };

    private readonly ContestReader _contestReader;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepository;
    private readonly IDbRepository<DataContext, DomainOfInfluenceCountingCircle> _domainOfInfluenceCountingCircleRepository;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepository;
    private readonly IDbRepository<DataContext, Contest> _contestRepository;
    private readonly PermissionService _permissionService;
    private readonly IMapper _mapper;
    private readonly PublisherConfig _config;
    private readonly ILogger<ResultExportTemplateReader> _logger;
    private readonly MessageConsumerHub<ProtocolExportStateChanged> _protocolExportStateChangedConsumer;
    private readonly IAuth _auth;
    private readonly ProportionalElectionReader _proportionalElectionReader;

    public ResultExportTemplateReader(
        ContestReader contestReader,
        PermissionService permissionService,
        IDbRepository<DataContext, CountingCircle> countingCircleRepository,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepository,
        IDbRepository<DataContext, Contest> contestRepository,
        IDbRepository<DataContext, DomainOfInfluenceCountingCircle> domainOfInfluenceCountingCircleRepository,
        IMapper mapper,
        PublisherConfig config,
        ILogger<ResultExportTemplateReader> logger,
        MessageConsumerHub<ProtocolExportStateChanged> protocolExportStateChangedConsumer,
        IAuth auth,
        ProportionalElectionReader proportionalElectionReader)
    {
        _contestReader = contestReader;
        _permissionService = permissionService;
        _mapper = mapper;
        _config = config;
        _logger = logger;
        _protocolExportStateChangedConsumer = protocolExportStateChangedConsumer;
        _countingCircleRepository = countingCircleRepository;
        _protocolExportRepository = protocolExportRepository;
        _domainOfInfluenceCountingCircleRepository = domainOfInfluenceCountingCircleRepository;
        _contestRepository = contestRepository;
        _auth = auth;
        _proportionalElectionReader = proportionalElectionReader;
    }

    public async Task<ExportTemplateContainer<ResultExportTemplate>> ListDataExportTemplates(Guid contestId, Guid? basisCountingCircleId)
    {
        var contest = await _contestReader.Get(contestId);
        var countingCircle = await GetCountingCircle(contestId, basisCountingCircleId);

        var templates = await FetchExportTemplates(
            contestId,
            basisCountingCircleId,
            new HashSet<ExportFileFormat> { ExportFileFormat.Csv, ExportFileFormat.Xml });
        return new ExportTemplateContainer<ResultExportTemplate>(contest, countingCircle, templates);
    }

    public async Task<ExportTemplateContainer<ProtocolExportTemplate>> ListProtocolExports(Guid contestId, Guid? basisCountingCircleId)
    {
        var contest = await _contestReader.Get(contestId);
        var countingCircle = await GetCountingCircle(contestId, basisCountingCircleId);

        var templates = await FetchExportTemplates(
            contestId,
            basisCountingCircleId,
            new HashSet<ExportFileFormat> { ExportFileFormat.Pdf });

        var protocolExportsTemplates = await AttachProtocolExportInfos(contestId, contest.TestingPhaseEnded, templates);

        return new ExportTemplateContainer<ProtocolExportTemplate>(contest, countingCircle, protocolExportsTemplates);
    }

    public async Task ListenToProtocolExportStateChanges(
        Guid contestId,
        Guid? basisCountingCircleId,
        Func<ProtocolExportStateChanged, Task> listener,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listening to protocol export state changes for contest with id {ContestId}", contestId);

        var contest = await _contestReader.Get(contestId);
        if (basisCountingCircleId.HasValue)
        {
            await _permissionService.CanReadBasisCountingCircle(basisCountingCircleId.Value, contestId);
        }

        var templates = await FetchExportTemplates(
            contestId,
            basisCountingCircleId,
            new HashSet<ExportFileFormat> { ExportFileFormat.Pdf });
        var protocolExportIds = templates
            .Select(t => AusmittlungUuidV5.BuildProtocolExport(contestId, contest.TestingPhaseEnded, t.ExportTemplateId))
            .ToHashSet();

        await _protocolExportStateChangedConsumer.Listen(
            e => protocolExportIds.Contains(e.ProtocolExportId),
            listener,
            cancellationToken);
    }

    internal async Task<IReadOnlyCollection<ResultExportTemplate>> FetchExportTemplates(
        Guid contestId,
        Guid? basisCountingCircleId,
        HashSet<ExportFileFormat>? formatsToFilter = null)
    {
        if (_config.DisableAllExports)
        {
            return Array.Empty<ResultExportTemplate>();
        }

        IEnumerable<TemplateModel> templates = TemplateRepository.GetByGenerator(VotingApp.VotingAusmittlung);
        templates = FilterDisabledTemplates(templates);

        if (formatsToFilter != null)
        {
            templates = templates.Where(t => formatsToFilter.Contains(t.Format));
        }

        var contest = await _contestRepository
            .Query()
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefaultAsync(x => x.Id == contestId)
            ?? throw new EntityNotFoundException(contestId);

        // activity protocol export should only be available if contest manager, testing phase ended and only for those with permission
        if (!_auth.HasPermission(Permissions.Export.ExportActivityProtocol) || !contest.TestingPhaseEnded || _auth.Tenant.Id != contest.DomainOfInfluence.SecureConnectId)
        {
            templates = templates.Where(t =>
                t.Key != AusmittlungCsvContestTemplates.ActivityProtocol.Key
                && t.Key != AusmittlungPdfContestTemplates.ActivityProtocol.Key);
        }

        // counting circle eVoting exports should only be available if eVoting is active for the counting circle
        var countingCircle = await GetCountingCircle(contestId, basisCountingCircleId);
        var ccDetails = countingCircle?.ContestDetails.FirstOrDefault();
        if (ccDetails?.EVoting == false || countingCircle?.EVoting == false)
        {
            templates = templates.Where(t => !_templateKeysCountingCircleEVoting.Contains(t.Key));
        }

        // political business eVoting exports should only be available if eVoting is active for contest and in case of communal domain of influence when its counting circle has e-voting
        var doiHasEVoting = await HasEVotingForDoiOnCountingCircle(contestId);
        if (!contest.EVoting || !doiHasEVoting)
        {
            templates = templates.Where(t => !_templateKeysPoliticalBusinessEVoting.Contains(t.Key));
        }

        if (!templates.Any())
        {
            return Array.Empty<ResultExportTemplate>();
        }

        var data = await (basisCountingCircleId.HasValue
            ? LoadExportDataForErfassung(contestId, basisCountingCircleId.Value)
            : LoadExportDataForMonitoring(contestId));
        var resultTemplates = templates.SelectMany(t => BuildResultExportTemplates(t, data));

        return resultTemplates
            .OrderBy(x => x.EntityDescription)
            .ThenBy(x => x.Description)
            .ToList();
    }

    /// <summary>
    /// Checks if there is any counting circle associated with the current tenant's domain of influence
    /// that has e-voting enabled, excluding certain higher authority domain of influence types.
    /// This fuction is for monitoring context only, because counting circle will be empty in this case.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value:
    /// true if there is at least one counting circle with e-voting enabled or domain of influence type is higher authority, otherwise false.
    /// </returns>
    private async Task<bool> HasEVotingForDoiOnCountingCircle(Guid contestId)
    {
        var doiTypesHigherAuthorities = new List<DomainOfInfluenceType>
        {
            DomainOfInfluenceType.Ch,
            DomainOfInfluenceType.Ct,
            DomainOfInfluenceType.Bz,
        };

        var countingCirles = await _domainOfInfluenceCountingCircleRepository
            .Query()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.CountingCircle)
            .Where(x => x.CountingCircle.SnapshotContestId == contestId
                && x.DomainOfInfluence.SecureConnectId == _auth.Tenant!.Id
                && x.DomainOfInfluence.Type >= DomainOfInfluenceType.Mu)
            .Select(x => x.CountingCircle)
            .ToListAsync();
        if (countingCirles.Count == 0)
        {
            return true;
        }

        return countingCirles.Any(cc => cc.EVoting);
    }

    private async Task<IReadOnlyCollection<ProtocolExportTemplate>> AttachProtocolExportInfos(
        Guid contestId,
        bool testingPhaseEnded,
        IReadOnlyCollection<ResultExportTemplate> templates)
    {
        var protocolTemplateIds = templates
            .Select(t => AusmittlungUuidV5.BuildProtocolExport(contestId, testingPhaseEnded, t.ExportTemplateId))
            .ToHashSet();

        var protocolExportsByExportTemplateId = await _protocolExportRepository.Query()
            .Where(x => protocolTemplateIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.ExportTemplateId);

        var protocolExportTemplates = templates
            .Select(x => new ProtocolExportTemplate(x))
            .ToList();

        foreach (var protocolExportTemplate in protocolExportTemplates)
        {
            if (protocolExportsByExportTemplateId.TryGetValue(protocolExportTemplate.Template.ExportTemplateId, out var protocolExport))
            {
                protocolExportTemplate.ProtocolExport = protocolExport;
            }
        }

        return protocolExportTemplates;
    }

    private async Task<CountingCircle?> GetCountingCircle(Guid contestId, Guid? basisCountingCircleId)
    {
        if (!basisCountingCircleId.HasValue)
        {
            return null;
        }

        await _permissionService.EnsureCanReadBasisCountingCircle(basisCountingCircleId.Value, contestId);
        return await _countingCircleRepository.Query()
            .Include(x => x.ContestDetails.Where(co => co.ContestId == contestId))
            .FirstOrDefaultAsync(c => c.BasisCountingCircleId == basisCountingCircleId && c.SnapshotContestId == contestId);
    }

    private async Task<ExportData> LoadExportDataForErfassung(Guid contestId, Guid basisCountingCircleId)
    {
        var politicalBusinesses = await _contestReader.GetAccessiblePoliticalBusinesses(basisCountingCircleId, contestId);
        var politicalBusinessesByType = politicalBusinesses
            .GroupBy(x => x.BusinessType)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<SimplePoliticalBusiness>)x.ToList());
        return new ExportData(
            basisCountingCircleId,
            politicalBusinessesByType,
            Array.Empty<PoliticalBusinessUnion>(),
            new Dictionary<Guid, ProportionalElectionMandateAlgorithm>());
    }

    private async Task<ExportData> LoadExportDataForMonitoring(Guid contestId)
    {
        _auth.EnsurePermission(Permissions.Export.ExportMonitoringData);
        var politicalBusinesses = await _contestReader.GetOwnedPoliticalBusinesses(contestId);
        var politicalBusinessesByType = politicalBusinesses
            .GroupBy(x => x.BusinessType)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<SimplePoliticalBusiness>)x.ToList());
        var politicalBusinessUnions = await _contestReader.ListPoliticalBusinessUnions(contestId);
        var proportionalElections = await _proportionalElectionReader.GetOwnedElections(contestId);
        return new ExportData(
            null,
            politicalBusinessesByType,
            politicalBusinessUnions.ToList(),
            proportionalElections.ToDictionary(pe => pe.Id, pe => pe.MandateAlgorithm));
    }

    private IEnumerable<ResultExportTemplate> BuildResultExportTemplates(
        TemplateModel template,
        ExportData data)
    {
        switch (template.ResultType)
        {
            // monitoring exports
            case ResultType.PoliticalBusinessResult:
            case ResultType.MultiplePoliticalBusinessesResult:
                return data.BasisCountingCircleId.HasValue
                    ? Enumerable.Empty<ResultExportTemplate>()
                    : ExpandPoliticalBusinesses(
                        template,
                        data.PoliticalBusinessesByType,
                        data.MandateAlgorithmByProportionalElectionId);

            // counting circle exports
            case ResultType.CountingCircleResult:
            case ResultType.MultiplePoliticalBusinessesCountingCircleResult:
                return !data.BasisCountingCircleId.HasValue
                    ? Enumerable.Empty<ResultExportTemplate>()
                    : ExpandPoliticalBusinesses(
                        template,
                        data.PoliticalBusinessesByType,
                        data.MandateAlgorithmByProportionalElectionId,
                        data.BasisCountingCircleId);

            case ResultType.Contest when !data.BasisCountingCircleId.HasValue:
                return new[] { new ResultExportTemplate(template, _permissionService.TenantId) };

            case ResultType.PoliticalBusinessUnionResult when !data.BasisCountingCircleId.HasValue:
                return ExpandPoliticalBusinessUnions(template, data.PoliticalBusinessUnions);

            default:
                return Enumerable.Empty<ResultExportTemplate>();
        }
    }

    private IEnumerable<ResultExportTemplate> ExpandPoliticalBusinessUnions(TemplateModel template, IEnumerable<PoliticalBusinessUnion> unions)
    {
        var filteredUnions = FilterProportionalElectionMandateAlgorithms(template, unions);
        return filteredUnions.Select(u => new ResultExportTemplate(
            template,
            _permissionService.TenantId,
            politicalBusinesses: u.PoliticalBusinesses.ToList(),
            politicalBusinessUnion: u));
    }

    private IEnumerable<ResultExportTemplate> ExpandPoliticalBusinesses(
        TemplateModel template,
        IReadOnlyDictionary<PoliticalBusinessType, IReadOnlyCollection<SimplePoliticalBusiness>> politicalBusinessesByType,
        IReadOnlyDictionary<Guid, ProportionalElectionMandateAlgorithm> mandateAlgorithmByProportionalElectionId,
        Guid? basisCountingCircleId = null)
    {
        var pbType = MapEntityTypeToPoliticalBusinessType(template.EntityType);
        if (!politicalBusinessesByType.TryGetValue(pbType, out var politicalBusinesses))
        {
            return Enumerable.Empty<ResultExportTemplate>();
        }

        var filteredPoliticalBusinesses = FilterPoliticalBusinessesForTemplate(template, politicalBusinesses, mandateAlgorithmByProportionalElectionId);

        if (template.ResultType
            is ResultType.MultiplePoliticalBusinessesResult
            or ResultType.MultiplePoliticalBusinessesCountingCircleResult)
        {
            return ExpandMultiplePoliticalBusinesses(template, filteredPoliticalBusinesses, basisCountingCircleId);
        }

        if (!filteredPoliticalBusinesses.Any())
        {
            return Enumerable.Empty<ResultExportTemplate>();
        }

        // expand templates for each individual political business
        return filteredPoliticalBusinesses
            .Select(pb => new ResultExportTemplate(
                template,
                _permissionService.TenantId,
                pb.Id,
                MapPoliticalBusinessToDescription(pb.DomainOfInfluence.Type, pb.BusinessType, template),
                countingCircleId: basisCountingCircleId,
                politicalBusinesses: new[] { pb }));
    }

    private IEnumerable<ResultExportTemplate> ExpandMultiplePoliticalBusinesses(
        TemplateModel template,
        IEnumerable<PoliticalBusiness> politicalBusinesses,
        Guid? basisCountingCircleId)
    {
        if (!politicalBusinesses.Any())
        {
            return Enumerable.Empty<ResultExportTemplate>();
        }

        if (!template.PerDomainOfInfluenceType)
        {
            return new[]
            {
                new ResultExportTemplate(
                    template,
                    _permissionService.TenantId,
                    politicalBusinesses: politicalBusinesses.ToList(),
                    countingCircleId: basisCountingCircleId),
            };
        }

        return politicalBusinesses.GroupBy(pb => pb.DomainOfInfluence.Type)
            .Where(x => template.MatchesDomainOfInfluenceType(_mapper.Map<Voting.Lib.VotingExports.Models.DomainOfInfluenceType>(x.Key)))
            .Select(g => new ResultExportTemplate(
                template,
                _permissionService.TenantId,
                description: MapMultiplePoliticalBusinessToDescription(g.ToList(), template),
                doiType: g.Key,
                countingCircleId: basisCountingCircleId,
                politicalBusinesses: g.ToList()));
    }

    private PoliticalBusinessType MapEntityTypeToPoliticalBusinessType(EntityType entityType)
    {
        return entityType switch
        {
            EntityType.Vote => PoliticalBusinessType.Vote,
            EntityType.MajorityElection => PoliticalBusinessType.MajorityElection,
            EntityType.SecondaryMajorityElection => PoliticalBusinessType.SecondaryMajorityElection,
            EntityType.ProportionalElection => PoliticalBusinessType.ProportionalElection,
            _ => PoliticalBusinessType.Unspecified,
        };
    }

    private IEnumerable<SimplePoliticalBusiness> FilterPoliticalBusinessesForTemplate(
        TemplateModel template,
        IEnumerable<SimplePoliticalBusiness> politicalBusinesses,
        IReadOnlyDictionary<Guid, ProportionalElectionMandateAlgorithm> mandateAlgorithmByProportionalElectionId)
    {
        politicalBusinesses = FilterDomainOfInfluenceType(template, politicalBusinesses);
        politicalBusinesses = FilterProportionalElectionMandateAlgorithm(template, politicalBusinesses, mandateAlgorithmByProportionalElectionId);
        politicalBusinesses = FilterMultipleCountingCircleResults(template, politicalBusinesses);
        return FilterInvalidVotes(template, politicalBusinesses);
    }

    private IEnumerable<SimplePoliticalBusiness> FilterDomainOfInfluenceType(TemplateModel template, IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
        => politicalBusinesses.Where(pb => template.MatchesDomainOfInfluenceType(_mapper.Map<Voting.Lib.VotingExports.Models.DomainOfInfluenceType>(pb.DomainOfInfluence.Type)));

    private IEnumerable<SimplePoliticalBusiness> FilterMultipleCountingCircleResults(TemplateModel template, IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
    {
        // These templates should only be displayed for political businesses with multiple counting circle results.
        // e.g. counting circle of a normal federal political business or communal political business as a "Stadtkreis" can view these protocols
        if (template.Key == AusmittlungPdfVoteTemplates.ResultProtocol.Key ||
            template.Key == AusmittlungPdfVoteTemplates.EndResultDomainOfInfluencesProtocol.Key ||
            template.Key == AusmittlungPdfMajorityElectionTemplates.EndResultDetailProtocol.Key)
        {
            return politicalBusinesses.Where(x => x.SimpleResults.Count > 1);
        }

        // These templates should always be displayed for canton ZH (VOTING-5358).
        if (template.Key == AusmittlungPdfMajorityElectionTemplates.CountingCircleProtocol.Key ||
            template.Key == AusmittlungPdfSecondaryMajorityElectionTemplates.CountingCircleProtocol.Key)
        {
            return politicalBusinesses.Where(x => x.SimpleResults.Count > 1 || x.DomainOfInfluence.Canton == DomainOfInfluenceCanton.Zh);
        }

        return politicalBusinesses;
    }

    private IEnumerable<TemplateModel> FilterDisabledTemplates(IEnumerable<TemplateModel> templates)
        => templates.Where(t => !_config.DisabledExportTemplateKeys.Contains(t.Key));

    private IEnumerable<SimplePoliticalBusiness> FilterInvalidVotes(TemplateModel template, IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
    {
        // some templates are only available for majority elections with enabled invalid votes
        if (!_templateKeysOnlyWithInvalidVotes.Contains(template.Key))
        {
            return politicalBusinesses;
        }

        return politicalBusinesses.Where(pb =>
            pb.Contest.CantonDefaults.MajorityElectionInvalidVotes);
    }

    private IEnumerable<PoliticalBusinessUnion> FilterProportionalElectionMandateAlgorithms(TemplateModel template, IEnumerable<PoliticalBusinessUnion> unions)
    {
        if (_templateKeysProportionalElectionMandateAlgorithmUnionDoubleProportional.Contains(template.Key))
        {
            return unions.Where(u =>
                u is ProportionalElectionUnion proportionalElectionUnion && proportionalElectionUnion
                    .ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElection.MandateAlgorithm
                    .IsDoubleProportional() == true);
        }

        if (_templateKeysProportionalElectionMandateAlgorithmHagenbachBischoff.Contains(template.Key))
        {
            return unions.Where(u =>
                u is ProportionalElectionUnion proportionalElectionUnion && proportionalElectionUnion
                    .ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElection.MandateAlgorithm ==
                    ProportionalElectionMandateAlgorithm.HagenbachBischoff);
        }

        return unions;
    }

    private IEnumerable<SimplePoliticalBusiness> FilterProportionalElectionMandateAlgorithm(
        TemplateModel template,
        IEnumerable<SimplePoliticalBusiness> politicalBusinesses,
        IReadOnlyDictionary<Guid, ProportionalElectionMandateAlgorithm> mandateAlgorithmByProportionalElectionId)
    {
        if (template.Key != AusmittlungPdfProportionalElectionTemplates.EndResultDoubleProportional.Key)
        {
            return politicalBusinesses;
        }

        return politicalBusinesses
            .Where(pb => mandateAlgorithmByProportionalElectionId.GetValueOrDefault(pb.Id).IsNonUnionDoubleProportional());
    }

    private string MapPoliticalBusinessToDescription(Data.Models.DomainOfInfluenceType doiType, PoliticalBusinessType pbType, TemplateModel template)
    {
        if (template.Format != ExportFileFormat.Pdf)
        {
            return template.Description;
        }

        var doiTypeDescription = doiType switch
        {
            Data.Models.DomainOfInfluenceType.Ch => Strings.Exports_DomainOfInfluenceType_Ch,
            Data.Models.DomainOfInfluenceType.Ct => Strings.Exports_DomainOfInfluenceType_Ct,
            Data.Models.DomainOfInfluenceType.Bz => Strings.Exports_DomainOfInfluenceType_Ct,
            Data.Models.DomainOfInfluenceType.Mu => Strings.Exports_DomainOfInfluenceType_Mu,
            Data.Models.DomainOfInfluenceType.Sk => Strings.Exports_DomainOfInfluenceType_Mu,
            _ => Strings.Exports_DomainOfInfluenceType_Other,
        };

        var pbTypeDescription = pbType switch
        {
            PoliticalBusinessType.Vote => Strings.Exports_Vote,
            PoliticalBusinessType.MajorityElection => Strings.Exports_MajorityElection,
            PoliticalBusinessType.SecondaryMajorityElection => Strings.Exports_SecondaryMajorityElection,
            PoliticalBusinessType.ProportionalElection => Strings.Exports_ProportionalElection,
            _ => string.Empty,
        };

        return $"{pbTypeDescription} {doiTypeDescription}: {template.Description}";
    }

    private string MapMultiplePoliticalBusinessToDescription(IReadOnlyList<PoliticalBusiness> pbs, TemplateModel template)
    {
        if (pbs.Count == 0)
        {
            return string.Empty;
        }

        var pbType = pbs.All(x => x.BusinessType == pbs[0].BusinessType) ? pbs[0].BusinessType : PoliticalBusinessType.Unspecified;
        return MapPoliticalBusinessToDescription(pbs[0].DomainOfInfluence.Type, pbType, template);
    }

    private record ExportData(
        Guid? BasisCountingCircleId,
        IReadOnlyDictionary<PoliticalBusinessType, IReadOnlyCollection<SimplePoliticalBusiness>> PoliticalBusinessesByType,
        IReadOnlyCollection<PoliticalBusinessUnion> PoliticalBusinessUnions,
        IReadOnlyDictionary<Guid, ProportionalElectionMandateAlgorithm> MandateAlgorithmByProportionalElectionId);
}
