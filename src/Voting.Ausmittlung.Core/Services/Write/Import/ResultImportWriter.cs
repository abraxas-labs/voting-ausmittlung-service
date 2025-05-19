// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public class ResultImportWriter
{
    private readonly ContestService _contestService;
    private readonly PermissionService _permissionService;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly ProportionalElectionResultImportWriter _proportionalElectionResultImportWriter;
    private readonly MajorityElectionResultImportWriter _majorityElectionResultImportWriter;
    private readonly SecondaryMajorityElectionResultImportWriter _secondaryMajorityElectionResultImportWriter;
    private readonly VoteResultImportWriter _voteResultImportWriter;
    private readonly Ech0110Deserializer _ech0110Deserializer;
    private readonly Ech0222Deserializer _ech0222Deserializer;

    public ResultImportWriter(ContestService contestService, PermissionService permissionService, IAggregateFactory aggregateFactory, IAggregateRepository aggregateRepository, IDbRepository<DataContext, Contest> contestRepo, ProportionalElectionResultImportWriter proportionalElectionResultImportWriter, MajorityElectionResultImportWriter majorityElectionResultImportWriter, SecondaryMajorityElectionResultImportWriter secondaryMajorityElectionResultImportWriter, VoteResultImportWriter voteResultImportWriter, Ech0110Deserializer ech0110Deserializer, Ech0222Deserializer ech0222Deserializer)
    {
        _contestService = contestService;
        _permissionService = permissionService;
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _contestRepo = contestRepo;
        _proportionalElectionResultImportWriter = proportionalElectionResultImportWriter;
        _majorityElectionResultImportWriter = majorityElectionResultImportWriter;
        _secondaryMajorityElectionResultImportWriter = secondaryMajorityElectionResultImportWriter;
        _voteResultImportWriter = voteResultImportWriter;
        _ech0110Deserializer = ech0110Deserializer;
        _ech0222Deserializer = ech0222Deserializer;
    }

    public async Task MapMajorityElectionWriteIns(
        Guid importId,
        Guid electionId,
        Guid basisCountingCircleId,
        PoliticalBusinessType pbType,
        IReadOnlyCollection<MajorityElectionWriteIn> mappings)
    {
        var aggregate = await _aggregateRepository.GetById<ResultImportAggregate>(importId);

        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(basisCountingCircleId, aggregate.ContestId);
        await _contestService.EnsureNotLocked(aggregate.ContestId);

        switch (pbType)
        {
            case PoliticalBusinessType.MajorityElection:
                await _majorityElectionResultImportWriter.ValidateWriteIns(electionId, basisCountingCircleId, mappings);
                break;
            case PoliticalBusinessType.SecondaryMajorityElection:
                await _secondaryMajorityElectionResultImportWriter.ValidateWriteIns(electionId, basisCountingCircleId, mappings);
                break;
            default:
                throw new ValidationException("Write-Ins are only available for majority elections!");
        }

        aggregate.MapMajorityElectionWriteIns(electionId, basisCountingCircleId, pbType, mappings);
        await _aggregateRepository.Save(aggregate);
    }

    public async Task ResetMajorityElectionWriteIns(
        Guid basisCountingCircleId,
        Guid electionId,
        PoliticalBusinessType pbType,
        Guid importId)
    {
        var aggregate = await _aggregateRepository.GetById<ResultImportAggregate>(importId);
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(basisCountingCircleId, aggregate.ContestId);
        await _contestService.EnsureNotLocked(aggregate.ContestId);

        switch (pbType)
        {
            case PoliticalBusinessType.MajorityElection:
                await _majorityElectionResultImportWriter.ValidateState(electionId, basisCountingCircleId);
                break;
            case PoliticalBusinessType.SecondaryMajorityElection:
                await _secondaryMajorityElectionResultImportWriter.ValidateState(electionId, basisCountingCircleId);
                break;
            default:
                throw new ValidationException("Write-Ins are only available for majority elections!");
        }

        aggregate.ResetMajorityElectionWriteIns(electionId, basisCountingCircleId, pbType);
        await _aggregateRepository.Save(aggregate);
    }

    internal VotingImport Deserialize(ResultImportMeta importMeta)
    {
        importMeta.Validate();

        var importData = _ech0222Deserializer.DeserializeXml(importMeta.ImportVersion, importMeta.Ech0222FileContent);
        if (importMeta.ContestId != importData.ContestId)
        {
            throw new ValidationException($"contestIds do not match: in import file {importData.ContestId}, in import metadata {importMeta.ContestId}");
        }

        if (importMeta.Ech0110FileContent != null)
        {
            _ech0110Deserializer.DeserializeXml(importMeta.Ech0110FileContent, importMeta.ContestId, importData);
        }

        return importData;
    }

    internal async Task<Contest> LoadContestForImport(ResultImportMeta importMeta)
    {
        var contest = await _contestRepo.Query()
                          .AsSplitQuery()
                          .Include(x => x.DomainOfInfluence)
                          .Include(x => x.SimplePoliticalBusinesses)
                          .Include(x => x.CountingCircleDetails.Where(d => !importMeta.BasisCountingCircleId.HasValue || d.CountingCircle.BasisCountingCircleId == importMeta.BasisCountingCircleId))
                          .ThenInclude(x => x.CountingCircle.ResponsibleAuthority)
                          .FirstOrDefaultAsync(x => x.Id == importMeta.ContestId)
                      ?? throw new EntityNotFoundException(nameof(Contest), importMeta.ContestId);

        _contestService.EnsureNotLocked(contest);
        return contest;
    }

    internal async Task<ResultImportAggregate> Import(
        VotingImport importData,
        ResultImportMeta importMeta,
        Contest contest,
        IEnumerable<IgnoredImportCountingCircle> ignoredCountingCircles)
    {
        var resultsByType = GroupByBusinessType(importData.PoliticalBusinessResults, contest.SimplePoliticalBusinesses);

        // The file names and eCH message IDs are currently concatenated. In the future, both the results and voting cards should be provided
        // in one eCH file. In the past, only a single eCH-0222 file (without eCH-0110) was used.
        // We do not want to add a new eCH-0110 file name field to the event, as it would be needed only temporarily.
        var aggregate = _aggregateFactory.New<ResultImportAggregate>();
        aggregate.Start(
            importMeta.GetUnifiedFileName(),
            importMeta.ImportType,
            importMeta.ContestId,
            importMeta.BasisCountingCircleId,
            importData.EchMessageId,
            ignoredCountingCircles);

        if (importData.VotingCards != null)
        {
            aggregate.ImportCountingCircleVotingCards(importData.VotingCards);
        }

        foreach (var (pbType, results) in resultsByType)
        {
            switch (pbType)
            {
                case PoliticalBusinessType.Vote:
                    await ImportVote(importMeta, results, aggregate);
                    break;
                case PoliticalBusinessType.MajorityElection:
                    await ImportMajorityElection(importMeta, results, aggregate);
                    break;
                case PoliticalBusinessType.ProportionalElection:
                    await ImportProportionalElection(importMeta, results, aggregate);
                    break;
                case PoliticalBusinessType.SecondaryMajorityElection:
                    await ImportSecondaryMajorityElection(importMeta, results, aggregate);
                    break;
            }
        }

        aggregate.Complete();
        return aggregate;
    }

    internal async Task CreateDeleteImportAndSave(BaseResultImportsAggregate resultImports)
    {
        var prevImport = await _aggregateRepository.GetById<ResultImportAggregate>(resultImports.LastImportId!.Value);
        await CreateDeleteImportAndSave(resultImports, prevImport);
    }

    internal async Task CreateDeleteImportAndSave(
        BaseResultImportsAggregate resultImports,
        ResultImportAggregate prevImport)
    {
        var deleteImport = _aggregateFactory.New<ResultImportAggregate>();
        deleteImport.DeleteData(prevImport.ContestId, prevImport.CountingCircleId, prevImport.ImportType);

        resultImports.CreateImport(resultImports.Id, deleteImport);
        prevImport.SucceedBy(deleteImport.Id, false);
        await _aggregateRepository.Save(resultImports);
        await _aggregateRepository.Save(prevImport);
        await _aggregateRepository.Save(deleteImport);
    }

    internal async Task SetSuccessorAndSave(
        Guid importsId,
        BaseResultImportsAggregate resultImports,
        ResultImportAggregate import,
        bool allowDeleted)
    {
        var prevImportId = resultImports.LastImportId;

        resultImports.CreateImport(importsId, import);
        await _aggregateRepository.Save(resultImports);

        if (!prevImportId.HasValue)
        {
            return;
        }

        var prevImport = await _aggregateRepository.GetById<ResultImportAggregate>(prevImportId.Value);
        prevImport.SucceedBy(import.Id, allowDeleted);
        await _aggregateRepository.Save(prevImport);
    }

    private Dictionary<PoliticalBusinessType, List<VotingImportPoliticalBusinessResult>> GroupByBusinessType(
        IEnumerable<VotingImportPoliticalBusinessResult> importedPoliticalBusinessResults,
        IEnumerable<SimplePoliticalBusiness> simplePoliticalBusinesses)
    {
        var simplePoliticalBusinessesById = simplePoliticalBusinesses.ToDictionary(x => x.Id);

        var resultsByPoliticalBusinessId = importedPoliticalBusinessResults
            .GroupBy(x => x.PoliticalBusinessId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var resultsByBusinessType = new Dictionary<PoliticalBusinessType, List<VotingImportPoliticalBusinessResult>>();
        foreach (var (politicalBusinessId, results) in resultsByPoliticalBusinessId)
        {
            var politicalBusinessType = GetPoliticalBusinessType(simplePoliticalBusinessesById, politicalBusinessId, results);

            if (!resultsByBusinessType.TryGetValue(politicalBusinessType, out var businessResults))
            {
                resultsByBusinessType[politicalBusinessType] = results;
            }
            else
            {
                businessResults.AddRange(results);
            }
        }

        return resultsByBusinessType;
    }

    private PoliticalBusinessType GetPoliticalBusinessType(
        Dictionary<Guid, SimplePoliticalBusiness> simplePoliticalBusinessesById,
        Guid politicalBusinessId,
        List<VotingImportPoliticalBusinessResult> results)
    {
        // Special case for votes, as we are always 100% sure that they are votes (there are no subtypes as for elections)
        // Cannot resolve the votes by their political business ID, as the IDs may not match to an existing vote (only the ballot IDs match)
        if (results.All(r => r.PoliticalBusinessType == PoliticalBusinessType.Vote))
        {
            return PoliticalBusinessType.Vote;
        }

        if (!simplePoliticalBusinessesById.TryGetValue(politicalBusinessId, out var simplePoliticalBusiness))
        {
            throw new EntityNotFoundException(nameof(PoliticalBusiness), politicalBusinessId);
        }

        // update the business type for further computations.
        foreach (var result in results)
        {
            result.PoliticalBusinessType = simplePoliticalBusiness.PoliticalBusinessType;
        }

        return simplePoliticalBusiness.PoliticalBusinessType;
    }

    private async Task ImportVote(
        ResultImportMeta importMeta,
        IEnumerable<VotingImportPoliticalBusinessResult> results,
        ResultImportAggregate aggregate)
    {
        var importData = _voteResultImportWriter.BuildImports(importMeta.ContestId, results.Cast<VotingImportVoteResult>().ToList());
        await foreach (var data in importData)
        {
            aggregate.ImportVoteResult(data);
        }
    }

    private async Task ImportProportionalElection(
        ResultImportMeta importMeta,
        IEnumerable<VotingImportPoliticalBusinessResult> results,
        ResultImportAggregate aggregate)
    {
        var importData = _proportionalElectionResultImportWriter.BuildImports(importMeta.ContestId, results.Cast<VotingImportElectionResult>().ToList());
        await foreach (var data in importData)
        {
            aggregate.ImportProportionalElectionResult(data);
        }
    }

    private async Task ImportMajorityElection(
        ResultImportMeta importMeta,
        IEnumerable<VotingImportPoliticalBusinessResult> results,
        ResultImportAggregate aggregate)
    {
        var importData = _majorityElectionResultImportWriter.BuildImports(importMeta, results.Cast<VotingImportElectionResult>().ToList());
        await foreach (var data in importData)
        {
            aggregate.ImportMajorityElectionResult(data);
        }
    }

    private async Task ImportSecondaryMajorityElection(
        ResultImportMeta importMeta,
        IEnumerable<VotingImportPoliticalBusinessResult> results,
        ResultImportAggregate aggregate)
    {
        var importData = _secondaryMajorityElectionResultImportWriter.BuildImports(importMeta, results.Cast<VotingImportElectionResult>().ToList());
        await foreach (var data in importData)
        {
            aggregate.ImportSecondaryMajorityElectionResult(data);
        }
    }
}
