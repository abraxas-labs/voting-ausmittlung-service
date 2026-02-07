// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Resources;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;
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
        AusmittlungPdfSecondaryMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key,
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

    private static readonly IReadOnlySet<string> _templateKeysProportionalElectionUnionMandateAlgorithmDoubleProportional = new HashSet<string>
    {
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultQuorumUnionListDoubleProportional.Key,
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultSubApportionmentDoubleProportional.Key,
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultSuperApportionmentDoubleProportional.Key,
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultNumberOfMandatesDoubleProportional.Key,
        AusmittlungPdfProportionalElectionTemplates.UnionEndResultCalculationDoubleProportional.Key,
    };

    private static readonly IReadOnlySet<string> _templateKeysProportionalElectionUnionMandateAlgorithmHagenbachBischoff = new HashSet<string>
    {
        AusmittlungPdfProportionalElectionTemplates.ListVotesPoliticalBusinessUnionEndResults.Key,
    };

    private static readonly IReadOnlySet<string> _templateKeysProportionalElectionMandateAlgorithmNonUnionDoubleProportional = new HashSet<string>
    {
        AusmittlungPdfProportionalElectionTemplates.EndResultDoubleProportional.Key,
    };

    private static readonly IReadOnlySet<string> _templateKeysProportionalElectionMandateAlgorithmHagenbachBischoff = new HashSet<string>
    {
        AusmittlungPdfProportionalElectionTemplates.EndResultCalculation.Key,
    };

    // These templates should only be displayed for political businesses with multiple counting circle results.
    // e.g. counting circle of a normal federal political business or communal political business as a "Stadtkreis" can view these protocols
    private static readonly IReadOnlySet<string> _templateKeysRequireMultipleCountingCircleResults = new HashSet<string>
    {
        AusmittlungPdfVoteTemplates.ResultProtocol.Key,
        AusmittlungPdfVoteTemplates.EndResultDomainOfInfluencesProtocol.Key,
        AusmittlungPdfMajorityElectionTemplates.EndResultDetailProtocol.Key,
        AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key,
        AusmittlungPdfSecondaryMajorityElectionTemplates.EndResultDetailProtocol.Key,
        AusmittlungPdfSecondaryMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key,
    };

    // These templates should only be displayed for the political business manager,
    // although the tenant can view the partial results of that political business.
    private static readonly IReadOnlySet<string> _templateKeysPartialResultsExcluded = new HashSet<string>
    {
        AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
    };

    private readonly ContestReader _contestReader;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepository;
    private readonly IDbRepository<DataContext, DomainOfInfluenceCountingCircle> _domainOfInfluenceCountingCircleRepository;
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepository;
    private readonly IDbRepository<DataContext, Contest> _contestRepository;
    private readonly PermissionService _permissionService;
    private readonly IMapper _mapper;
    private readonly PublisherConfig _config;
    private readonly IAuth _auth;
    private readonly IDbRepository<DataContext, ProportionalElection> _proportionalElectionRepo;

    public ResultExportTemplateReader(
        ContestReader contestReader,
        PermissionService permissionService,
        IDbRepository<DataContext, CountingCircle> countingCircleRepository,
        IDbRepository<DataContext, ProtocolExport> protocolExportRepository,
        IDbRepository<DataContext, Contest> contestRepository,
        IDbRepository<DataContext, DomainOfInfluenceCountingCircle> domainOfInfluenceCountingCircleRepository,
        IMapper mapper,
        PublisherConfig config,
        IAuth auth,
        IDbRepository<DataContext, ProportionalElection> proportionalElectionRepo)
    {
        _contestReader = contestReader;
        _permissionService = permissionService;
        _mapper = mapper;
        _config = config;
        _countingCircleRepository = countingCircleRepository;
        _protocolExportRepository = protocolExportRepository;
        _domainOfInfluenceCountingCircleRepository = domainOfInfluenceCountingCircleRepository;
        _contestRepository = contestRepository;
        _auth = auth;
        _proportionalElectionRepo = proportionalElectionRepo;
    }

    public async Task<ExportTemplateContainer<ResultExportTemplate>> ListDataExportTemplates(Guid contestId, Guid? basisCountingCircleId, bool accessiblePbs = false)
    {
        var contest = await _contestReader.Get(contestId);
        var countingCircle = await GetCountingCircle(contestId, basisCountingCircleId);

        var templates = await FetchExportTemplates(
            contestId,
            basisCountingCircleId,
            new HashSet<ExportFileFormat> { ExportFileFormat.Csv, ExportFileFormat.Xml },
            accessiblePbs);
        return new ExportTemplateContainer<ResultExportTemplate>(contest, countingCircle, templates);
    }

    public async Task<ExportTemplateContainer<ProtocolExportTemplate>> ListProtocolExports(Guid contestId, Guid? basisCountingCircleId, bool accessiblePbs = false)
    {
        var contest = await _contestReader.Get(contestId);
        var countingCircle = await GetCountingCircle(contestId, basisCountingCircleId);

        var templates = await FetchExportTemplates(
            contestId,
            basisCountingCircleId,
            new HashSet<ExportFileFormat> { ExportFileFormat.Pdf },
            accessiblePbs);

        var protocolExportsTemplates = await AttachProtocolExportInfos(contestId, contest.TestingPhaseEnded, templates);

        return new ExportTemplateContainer<ProtocolExportTemplate>(contest, countingCircle, protocolExportsTemplates);
    }

    internal async Task<IReadOnlySet<Guid>> GetReadableProtocolExportIds(
        Guid contestId,
        Guid? basisCountingCircleId,
        HashSet<ExportFileFormat>? formatsToFilter = null)
    {
        var contest = await _contestRepository
                          .Query()
                          .Include(x => x.DomainOfInfluence)
                          .FirstOrDefaultAsync(x => x.Id == contestId)
                      ?? throw new EntityNotFoundException(contestId);
        var resultExportTemplates = await FetchExportTemplates(contest, basisCountingCircleId, formatsToFilter);
        return resultExportTemplates
            .Select(t => AusmittlungUuidV5.BuildProtocolExport(contestId, contest.TestingPhaseEnded, t.ExportTemplateId))
            .ToHashSet();
    }

    internal async Task<IReadOnlyCollection<ResultExportTemplate>> FetchExportTemplates(
        Guid contestId,
        Guid? basisCountingCircleId,
        HashSet<ExportFileFormat>? formatsToFilter = null,
        bool accessiblePbs = false)
    {
        var contest = await _contestRepository
                          .Query()
                          .Include(x => x.DomainOfInfluence)
                          .FirstOrDefaultAsync(x => x.Id == contestId)
                      ?? throw new EntityNotFoundException(contestId);
        return await FetchExportTemplates(contest, basisCountingCircleId, formatsToFilter, accessiblePbs);
    }

    internal async Task<IReadOnlyCollection<ResultExportTemplate>> FetchExportTemplates(
        Contest contest,
        Guid? basisCountingCircleId,
        HashSet<ExportFileFormat>? formatsToFilter = null,
        bool accessiblePbs = false)
    {
        if (_config.DisableAllExports)
        {
            return [];
        }

        IEnumerable<TemplateModel> templates = TemplateRepository.GetByGenerator(VotingApp.VotingAusmittlung);
        templates = FilterDisabledTemplates(templates);

        if (formatsToFilter != null)
        {
            templates = templates.Where(t => formatsToFilter.Contains(t.Format));
        }

        // activity protocol export should only be available if contest manager, testing phase ended and only for those with permission
        if (!_auth.HasPermission(Permissions.Export.ExportActivityProtocol) || !contest.TestingPhaseEnded || _auth.Tenant.Id != contest.DomainOfInfluence.SecureConnectId)
        {
            templates = templates.Where(t =>
                t.Key != AusmittlungCsvContestTemplates.ActivityProtocol.Key
                && t.Key != AusmittlungPdfContestTemplates.ActivityProtocol.Key);
        }

        // restrict exporting of eCH-0252 "Panaschierstatistik" to those with permission
        if (!_auth.HasPermission(Permissions.Export.ExportEch0252ProportionalElectionWithCandidateListResultsInfo)
            || _auth.Tenant.Id != contest.DomainOfInfluence.SecureConnectId)
        {
            templates = templates.Where(t => t.Key != AusmittlungXmlContestTemplates.ProportionalElectionResultsWithCandidateListResultsInfoEch0252.Key);
        }

        // counting circle eVoting exports should only be available if eVoting is active for the counting circle
        var countingCircle = await GetCountingCircle(contest.Id, basisCountingCircleId);
        var ccDetails = countingCircle?.ContestDetails.FirstOrDefault();
        if (ccDetails?.EVoting == false || countingCircle?.EVoting == false)
        {
            templates = templates.Where(t => !_templateKeysCountingCircleEVoting.Contains(t.Key));
        }

        // political business eVoting exports should only be available if eVoting is active for contest and in case of communal domain of influence when its counting circle has e-voting
        var doiHasEVoting = await HasEVotingForDoiOnCountingCircle(contest.Id);
        if (!contest.EVoting || !doiHasEVoting)
        {
            templates = templates.Where(t => !_templateKeysPoliticalBusinessEVoting.Contains(t.Key));
        }

        if (!templates.Any())
        {
            return [];
        }

        var data = await (basisCountingCircleId.HasValue
            ? LoadExportDataForErfassung(contest.Id, basisCountingCircleId.Value)
            : LoadExportDataForMonitoring(contest.Id, accessiblePbs));
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
            [],
            new Dictionary<Guid, ProportionalElectionMandateAlgorithm>(),
            true);
    }

    private async Task<ExportData> LoadExportDataForMonitoring(Guid contestId, bool accessiblePbs)
    {
        _auth.EnsurePermission(Permissions.Export.ExportMonitoringData);
        var politicalBusinesses = await (accessiblePbs ? _contestReader.GetAccessiblePoliticalBusinesses(contestId) : _contestReader.GetOwnedPoliticalBusinesses(contestId));
        var politicalBusinessesByType = politicalBusinesses
            .GroupBy(x => x.BusinessType)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<SimplePoliticalBusiness>)x.ToList());
        var peIds = politicalBusinesses
            .Where(pb => pb.BusinessType == PoliticalBusinessType.ProportionalElection)
            .Select(pb => pb.Id)
            .ToList();

        var politicalBusinessUnions = await _contestReader.ListPoliticalBusinessUnions(contestId);
        var proportionalElections = _proportionalElectionRepo.Query().Where(pe => peIds.Contains(pe.Id));

        return new ExportData(
            null,
            politicalBusinessesByType,
            politicalBusinessUnions.ToList(),
            proportionalElections.ToDictionary(pe => pe.Id, pe => pe.MandateAlgorithm),
            accessiblePbs);
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
                    ? []
                    : ExpandPoliticalBusinesses(
                        template,
                        data.PoliticalBusinessesByType,
                        data.MandateAlgorithmByProportionalElectionId,
                        data.HasAllAccessiblePoliticalBusinesses);

            // counting circle exports
            case ResultType.CountingCircleResult:
            case ResultType.MultiplePoliticalBusinessesCountingCircleResult:
                return !data.BasisCountingCircleId.HasValue
                    ? []
                    : ExpandPoliticalBusinesses(
                        template,
                        data.PoliticalBusinessesByType,
                        data.MandateAlgorithmByProportionalElectionId,
                        data.HasAllAccessiblePoliticalBusinesses,
                        data.BasisCountingCircleId);

            case ResultType.Contest when !data.BasisCountingCircleId.HasValue:
                return [new ResultExportTemplate(template, _permissionService.TenantId)];

            case ResultType.PoliticalBusinessUnionResult when !data.BasisCountingCircleId.HasValue:
                return ExpandPoliticalBusinessUnions(template, data.PoliticalBusinessUnions);

            default:
                return [];
        }
    }

    private IEnumerable<ResultExportTemplate> ExpandPoliticalBusinessUnions(TemplateModel template, IEnumerable<PoliticalBusinessUnion> unions)
    {
        var filteredUnions = FilterProportionalElectionUnionMandateAlgorithms(template, unions);
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
        bool hasAllAccessiblePoliticalBusinesses,
        Guid? basisCountingCircleId = null)
    {
        var pbType = MapEntityTypeToPoliticalBusinessType(template.EntityType);
        if (!politicalBusinessesByType.TryGetValue(pbType, out var politicalBusinesses))
        {
            return [];
        }

        var filteredPoliticalBusinesses = FilterPoliticalBusinessesForTemplate(template, politicalBusinesses, mandateAlgorithmByProportionalElectionId, hasAllAccessiblePoliticalBusinesses);
        if (!filteredPoliticalBusinesses.Any())
        {
            return [];
        }

        if (template.ResultType
            is ResultType.MultiplePoliticalBusinessesResult
            or ResultType.MultiplePoliticalBusinessesCountingCircleResult)
        {
            return ExpandMultiplePoliticalBusinesses(template, filteredPoliticalBusinesses, basisCountingCircleId);
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
        if (template.PerDomainOfInfluenceType)
        {
            foreach (var group in politicalBusinesses.GroupBy(pb => pb.DomainOfInfluence.Type))
            {
                if (!template.MatchesDomainOfInfluenceType(_mapper.Map<Voting.Lib.VotingExports.Models.DomainOfInfluenceType>(group.Key)))
                {
                    continue;
                }

                var pbs = group.ToList();
                var pbType = pbs.All(x => x.BusinessType == pbs[0].BusinessType) ? pbs[0].BusinessType : PoliticalBusinessType.Unspecified;
                yield return new ResultExportTemplate(
                    template,
                    _permissionService.TenantId,
                    description: MapPoliticalBusinessToDescription(group.Key, pbType, template),
                    doiType: group.Key,
                    countingCircleId: basisCountingCircleId,
                    politicalBusinesses: pbs);
            }

            yield break;
        }

        if (template.PerDomainOfInfluence)
        {
            var politicalBusinessesGroupedByDomainOfInfluence = politicalBusinesses.GroupBy(pb => pb.DomainOfInfluence.BasisDomainOfInfluenceId);
            var hasMultipleCantonalReports = HasMultipleReportsPerGroupedCanton(politicalBusinesses);
            var hasMultipleMunicipalReports = HasMultipleReportsPerGroupedMunicipality(politicalBusinesses);
            var hasMultipleOtherReports = HasMultipleReportsPerGroupedOther(politicalBusinesses);
            foreach (var group in politicalBusinessesGroupedByDomainOfInfluence)
            {
                var pbs = group.ToList();
                yield return new ResultExportTemplate(
                    template,
                    _permissionService.TenantId,
                    description: MapPoliticalBusinessToDescription(pbs[0].DomainOfInfluence, pbs[0].BusinessType, template, hasMultipleCantonalReports, hasMultipleMunicipalReports, hasMultipleOtherReports),
                    countingCircleId: basisCountingCircleId,
                    politicalBusinesses: pbs,
                    domainOfInfluenceId: group.Key);
            }

            yield break;
        }

        yield return new ResultExportTemplate(
            template,
            _permissionService.TenantId,
            countingCircleId: basisCountingCircleId,
            politicalBusinesses: politicalBusinesses.ToList());

        static bool HasMultipleReportsPerGroupedCanton(IEnumerable<PoliticalBusiness> politicalBusinesses) => politicalBusinesses.Where(pb => pb.DomainOfInfluence.Type == DomainOfInfluenceType.Ct || pb.DomainOfInfluence.Type == DomainOfInfluenceType.Bz).GroupBy(pb => pb.DomainOfInfluence.BasisDomainOfInfluenceId).Count() > 1;
        static bool HasMultipleReportsPerGroupedMunicipality(IEnumerable<PoliticalBusiness> politicalBusinesses) => politicalBusinesses.Where(pb => pb.DomainOfInfluence.Type == DomainOfInfluenceType.Mu || pb.DomainOfInfluence.Type == DomainOfInfluenceType.Sk).GroupBy(pb => pb.DomainOfInfluence.BasisDomainOfInfluenceId).Count() > 1;
        static bool HasMultipleReportsPerGroupedOther(IEnumerable<PoliticalBusiness> politicalBusinesses) => politicalBusinesses.Where(pb => !(pb.DomainOfInfluence.Type == DomainOfInfluenceType.Mu || pb.DomainOfInfluence.Type == DomainOfInfluenceType.Sk || pb.DomainOfInfluence.Type == DomainOfInfluenceType.Ct || pb.DomainOfInfluence.Type == DomainOfInfluenceType.Bz || pb.DomainOfInfluence.Type == DomainOfInfluenceType.Ch)).GroupBy(pb => pb.DomainOfInfluence.BasisDomainOfInfluenceId).Count() > 1;
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
        IReadOnlyDictionary<Guid, ProportionalElectionMandateAlgorithm> mandateAlgorithmByProportionalElectionId,
        bool hasAllAccessiblePoliticalBusinesses)
    {
        politicalBusinesses = FilterDomainOfInfluenceType(template, politicalBusinesses);
        politicalBusinesses = FilterProportionalElectionMandateAlgorithm(template, politicalBusinesses, mandateAlgorithmByProportionalElectionId);
        politicalBusinesses = FilterMultipleCountingCircleResults(template, politicalBusinesses);
        politicalBusinesses = FilterViewablePartialResults(template, politicalBusinesses, hasAllAccessiblePoliticalBusinesses);
        return FilterInvalidVotes(template, politicalBusinesses);
    }

    private IEnumerable<SimplePoliticalBusiness> FilterDomainOfInfluenceType(TemplateModel template, IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
        => politicalBusinesses.Where(pb => template.MatchesDomainOfInfluenceType(_mapper.Map<Lib.VotingExports.Models.DomainOfInfluenceType>(pb.DomainOfInfluence.Type)));

    private IEnumerable<SimplePoliticalBusiness> FilterMultipleCountingCircleResults(TemplateModel template, IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
    {
        if (_templateKeysRequireMultipleCountingCircleResults.Contains(template.Key))
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

    private IEnumerable<SimplePoliticalBusiness> FilterViewablePartialResults(TemplateModel template, IEnumerable<SimplePoliticalBusiness> politicalBusinesses, bool hasAllAccessiblePoliticalBusinesses)
    {
        if (!hasAllAccessiblePoliticalBusinesses && _templateKeysPartialResultsExcluded.Contains(template.Key))
        {
            return politicalBusinesses.Where(x => x.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id);
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

    private IEnumerable<PoliticalBusinessUnion> FilterProportionalElectionUnionMandateAlgorithms(TemplateModel template, IEnumerable<PoliticalBusinessUnion> unions)
    {
        if (_templateKeysProportionalElectionUnionMandateAlgorithmDoubleProportional.Contains(template.Key))
        {
            return unions.Where(u =>
                u is ProportionalElectionUnion proportionalElectionUnion && proportionalElectionUnion
                    .ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElection.MandateAlgorithm
                    .IsDoubleProportional() == true);
        }

        if (_templateKeysProportionalElectionUnionMandateAlgorithmHagenbachBischoff.Contains(template.Key))
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
        if (_templateKeysProportionalElectionMandateAlgorithmHagenbachBischoff.Contains(template.Key))
        {
            return politicalBusinesses
                .Where(pb => mandateAlgorithmByProportionalElectionId.GetValueOrDefault(pb.Id) == ProportionalElectionMandateAlgorithm.HagenbachBischoff);
        }

        if (_templateKeysProportionalElectionMandateAlgorithmNonUnionDoubleProportional.Contains(template.Key))
        {
            return politicalBusinesses
                .Where(pb => mandateAlgorithmByProportionalElectionId.GetValueOrDefault(pb.Id).IsNonUnionDoubleProportional());
        }

        return politicalBusinesses;
    }

    private string MapPoliticalBusinessToDescription(DomainOfInfluenceType doiType, PoliticalBusinessType pbType, TemplateModel template)
    {
        if (template.Format != ExportFileFormat.Pdf)
        {
            return template.Description;
        }

        var doiTypeDescription = doiType switch
        {
            DomainOfInfluenceType.Ch => Strings.Exports_DomainOfInfluenceType_Ch,
            DomainOfInfluenceType.Ct => Strings.Exports_DomainOfInfluenceType_Ct,
            DomainOfInfluenceType.Bz => Strings.Exports_DomainOfInfluenceType_Ct,
            DomainOfInfluenceType.Mu => Strings.Exports_DomainOfInfluenceType_Mu,
            DomainOfInfluenceType.Sk => Strings.Exports_DomainOfInfluenceType_Mu,
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

    private string MapPoliticalBusinessToDescription(DomainOfInfluence doi, PoliticalBusinessType pbType, TemplateModel template, bool hasMultipleCt, bool hasMultipleMu, bool hasMulitpleOther)
    {
        if (template.Format != ExportFileFormat.Pdf)
        {
            return template.Description;
        }

        var pbTypeDescription = pbType switch
        {
            PoliticalBusinessType.Vote => Strings.Exports_Vote,
            PoliticalBusinessType.MajorityElection => Strings.Exports_MajorityElection,
            PoliticalBusinessType.SecondaryMajorityElection => Strings.Exports_SecondaryMajorityElection,
            PoliticalBusinessType.ProportionalElection => Strings.Exports_ProportionalElection,
            _ => string.Empty,
        };
        var doiTypeDescription = doi.Type switch
        {
            DomainOfInfluenceType.Ch => Strings.Exports_DomainOfInfluenceType_Ch,
            DomainOfInfluenceType.Ct => hasMultipleCt
                ? $"{Strings.Exports_DomainOfInfluenceType_Ct} {doi.ShortName}"
                : Strings.Exports_DomainOfInfluenceType_Ct,
            DomainOfInfluenceType.Bz => hasMultipleCt
                ? $"{Strings.Exports_DomainOfInfluenceType_Ct} {doi.ShortName}"
                : Strings.Exports_DomainOfInfluenceType_Ct,
            DomainOfInfluenceType.Mu => hasMultipleMu
                ? $"{Strings.Exports_DomainOfInfluenceType_Mu} {doi.ShortName}"
                : Strings.Exports_DomainOfInfluenceType_Mu,
            DomainOfInfluenceType.Sk => hasMultipleMu
                ? $"{Strings.Exports_DomainOfInfluenceType_Mu} {doi.ShortName}"
                : Strings.Exports_DomainOfInfluenceType_Mu,
            _ => hasMulitpleOther
                ? $"{Strings.Exports_DomainOfInfluenceType_Other} {doi.ShortName}"
                : Strings.Exports_DomainOfInfluenceType_Other,
        };

        return $"{pbTypeDescription} {doiTypeDescription}: {template.Description}";
    }

    private record ExportData(
        Guid? BasisCountingCircleId,
        IReadOnlyDictionary<PoliticalBusinessType, IReadOnlyCollection<SimplePoliticalBusiness>> PoliticalBusinessesByType,
        IReadOnlyCollection<PoliticalBusinessUnion> PoliticalBusinessUnions,
        IReadOnlyDictionary<Guid, ProportionalElectionMandateAlgorithm> MandateAlgorithmByProportionalElectionId,
        bool HasAllAccessiblePoliticalBusinesses);
}
