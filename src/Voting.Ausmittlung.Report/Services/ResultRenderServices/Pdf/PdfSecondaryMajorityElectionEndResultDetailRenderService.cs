// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfSecondaryMajorityElectionEndResultDetailRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _repo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResult> _resultRepo;
    private readonly IMapper _mapper;
    private readonly MajorityElectionDomainOfInfluenceResultBuilder _doiResultBuilder;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly IClock _clock;

    public PdfSecondaryMajorityElectionEndResultDetailRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, SecondaryMajorityElection> repo,
        IDbRepository<DataContext, SecondaryMajorityElectionResult> resultRepo,
        IMapper mapper,
        MajorityElectionDomainOfInfluenceResultBuilder doiResultBuilder,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IClock clock)
    {
        _templateService = templateService;
        _repo = repo;
        _resultRepo = resultRepo;
        _mapper = mapper;
        _doiResultBuilder = doiResultBuilder;
        _ccDetailsRepo = ccDetailsRepo;
        _clock = clock;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var secondaryData = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.EndResult!.PrimaryMajorityElectionEndResult)
            .Include(x => x.EndResult!.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.PrimaryMajorityElection.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.PrimaryMajorityElection.DomainOfInfluence.Details!.CountOfVotersInformationSubTotals)
            .Include(x => x.PrimaryMajorityElection.Contest.DomainOfInfluence)
            .Include(x => x.PrimaryMajorityElection.Contest.Translations)
            .Include(x => x.PrimaryMajorityElection.Contest.CantonDefaults)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}");

        var isPartialResult = secondaryData.DomainOfInfluence.SecureConnectId != ctx.TenantId;
        if (isPartialResult && ctx.ViewablePartialResultsCountingCircleIds?.Count == 0)
        {
            throw new ValidationException("invalid partial result without any viewable counting circle ids");
        }

        var secondaryResults = await _resultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.PrimaryResult.CountingCircle.ContestDetails)
            .ThenInclude(x => x.VotingCards)
            .Include(x => x.PrimaryResult.CountingCircle.ContestDetails)
            .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.CandidateResults)
            .ThenInclude(x => x.Candidate.Translations)
            .Where(x => x.SecondaryMajorityElectionId == ctx.PoliticalBusinessId && (!isPartialResult || ctx.ViewablePartialResultsCountingCircleIds!.Contains(x.PrimaryResult.CountingCircleId)))
            .ToListAsync(ct);

        // The simplest approach to handle partial results and domain of influence result building is to
        // handle secondary candidates like they are primary candidates.
        var data = MapSecondaryElectionToPrimaryElection(secondaryData, secondaryResults, ctx.PoliticalBusinessId);
        var results = data.Results.ToList();

        if (data.Results.Count == 0)
        {
            throw new ValidationException($"no results found for: {nameof(ctx.PoliticalBusinessId)}: {ctx.PoliticalBusinessId}");
        }

        data.MoveECountingToConventional();

        if (isPartialResult)
        {
            data.EndResult = PartialEndResultUtils.MergeIntoPartialEndResult(data, results);

            foreach (var candidateEndResult in data.EndResult.CandidateEndResults)
            {
                candidateEndResult.State = MajorityElectionCandidateEndResultState.Pending;
            }

            data.DomainOfInfluence.Details = AggregatedContestCountingCircleDetailsBuilder.BuildDomainOfInfluenceDetails(results
                .SelectMany(x => x.CountingCircle!.ContestDetails)
                .DistinctBy(x => x.CountingCircleId)
                .ToList());
        }

        var ccDetailsList = await _ccDetailsRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Where(x => x.ContestId == data.ContestId)
            .ToListAsync(ct);

        var (doiResults, notAssignableResult, aggregatedResult) = await _doiResultBuilder.BuildResults(
                data,
                ccDetailsList,
                ctx.TenantId ?? data.DomainOfInfluence.SecureConnectId);

        // don't map results
        data.Results = new List<MajorityElectionResult>();

        var pdfCcDetails = _mapper.Map<List<PdfContestCountingCircleDetails>>(ccDetailsList);
        foreach (var details in pdfCcDetails)
        {
            PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(details, data.DomainOfInfluence);

            // we don't need this data in the xml
            details.VotingCards = new List<PdfVotingCardResultDetail>();
            details.CountOfVotersInformationSubTotals = new List<PdfCountOfVotersInformationSubTotal>();
        }

        var majorityElection = _mapper.Map<PdfMajorityElection>(data);
        majorityElection.EmptyVoteCountDisabled = false;
        majorityElection.DomainOfInfluenceResults = _mapper.Map<List<PdfMajorityElectionDomainOfInfluenceResult>>(doiResults);

        // only show cc results in election which are not included in doi results (ex: reporting level 1 and cc Auslandschweizer)
        majorityElection.Results = _mapper.Map<List<PdfMajorityElectionResult>>(notAssignableResult.Results);

        var doiCcResults = majorityElection.DomainOfInfluenceResults
            .Where(x => x.Results != null)
            .SelectMany(x => x.Results!)
            .Concat(majorityElection.Results)
            .ToList();

        majorityElection.AggregatedDomainOfInfluenceResult = _mapper.Map<PdfMajorityElectionDomainOfInfluenceResult>(aggregatedResult);
        majorityElection.AggregatedDomainOfInfluenceResult.Results = null;

        PdfCountingCircleResultUtil.MapContestCountingCircleDetailsToResults(pdfCcDetails, doiCcResults);
        PdfCountingCircleResultUtil.RemoveContactPersonDetails(doiCcResults);

        OrderCandidateResults(majorityElection);

        // reset the domain of influence on the result, since this is a single domain of influence report
        var domainOfInfluence = majorityElection.DomainOfInfluence;
        domainOfInfluence!.Details ??= new PdfContestDomainOfInfluenceDetails();
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(domainOfInfluence.Details, data.DomainOfInfluence);

        // we don't need this data in the xml
        domainOfInfluence.Details.VotingCards = new List<PdfVotingCardResultDetail>();
        domainOfInfluence.Details.CountOfVotersInformationSubTotals = new List<PdfCountOfVotersInformationSubTotal>();
        majorityElection.DomainOfInfluence = null;

        var contest = _mapper.Map<PdfContest>(data.Contest);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
            Contest = contest,
            DomainOfInfluence = domainOfInfluence,
            MajorityElections = new List<PdfMajorityElection>
            {
                majorityElection,
            },
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            data.ShortDescription,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }

    private void OrderCandidateResults(PdfMajorityElection majorityElection)
    {
        foreach (var ccResult in majorityElection.Results!)
        {
            ccResult.CandidateResults = ccResult.CandidateResults!
                .OrderBy(x => x.Candidate!.Position)
                .ToList();
        }

        majorityElection.EndResult!.CandidateEndResults = majorityElection.EndResult.CandidateEndResults!
            .OrderBy(x => x.Candidate!.Position)
            .ToList();
    }

    private MajorityElection MapSecondaryElectionToPrimaryElection(
        SecondaryMajorityElection secondaryElection,
        List<SecondaryMajorityElectionResult> secondaryResults,
        Guid secondaryMajorityElectionId)
    {
        var primaryElection = _mapper.Map<MajorityElection>(secondaryElection);

        // We need certain data from the primary end result, like total count of voters.
        primaryElection.EndResult = secondaryElection.EndResult!.PrimaryMajorityElectionEndResult!;
        primaryElection.EndResult.CandidateEndResults = _mapper.Map<List<MajorityElectionCandidateEndResult>>(
            secondaryElection.EndResult.CandidateEndResults);

        primaryElection.EndResult.ConventionalSubTotal = secondaryElection.EndResult.ConventionalSubTotal;
        primaryElection.EndResult.EVotingSubTotal = secondaryElection.EndResult.EVotingSubTotal;
        primaryElection.EndResult.ECountingSubTotal = secondaryElection.EndResult.ECountingSubTotal;

        primaryElection.Results = new List<MajorityElectionResult>();
        foreach (var secondaryResult in secondaryResults)
        {
            var primaryResult = secondaryResult.PrimaryResult;
            primaryResult.CandidateResults = _mapper.Map<List<MajorityElectionCandidateResult>>(secondaryResult.CandidateResults);

            primaryResult.ConventionalSubTotal = secondaryResult.ConventionalSubTotal;
            primaryResult.EVotingSubTotal = secondaryResult.EVotingSubTotal;
            primaryResult.ECountingSubTotal = secondaryResult.ECountingSubTotal;

            primaryElection.Results.Add(primaryResult);
        }

        return primaryElection;
    }
}
