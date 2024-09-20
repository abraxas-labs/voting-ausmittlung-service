// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

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
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfMajorityElectionEndResultDetailRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _repo;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;
    private readonly IMapper _mapper;
    private readonly MajorityElectionDomainOfInfluenceResultBuilder _doiResultBuilder;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;

    public PdfMajorityElectionEndResultDetailRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElection> repo,
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IMapper mapper,
        MajorityElectionDomainOfInfluenceResultBuilder doiResultBuilder,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo)
    {
        _templateService = templateService;
        _repo = repo;
        _resultRepo = resultRepo;
        _mapper = mapper;
        _doiResultBuilder = doiResultBuilder;
        _ccDetailsRepo = ccDetailsRepo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var data = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.EndResult!.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.Contest.DomainOfInfluence)
            .Include(x => x.Contest.Translations)
            .Include(x => x.Contest.CantonDefaults)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}");

        var isPartialResult = data.DomainOfInfluence.SecureConnectId != ctx.TenantId;
        if (isPartialResult && ctx.ViewablePartialResultsCountingCircleIds?.Count == 0)
        {
            throw new ValidationException("invalid partial result without any viewable counting circle ids");
        }

        var results = await _resultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle.ContestDetails)
            .ThenInclude(x => x.VotingCards)
            .Include(x => x.CountingCircle.ContestDetails)
            .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.CandidateResults)
            .ThenInclude(x => x.Candidate.Translations)
            .Where(x => x.MajorityElectionId == ctx.PoliticalBusinessId && (!isPartialResult || ctx.ViewablePartialResultsCountingCircleIds!.Contains(x.CountingCircleId)))
            .ToListAsync(ct);

        if (results.Count == 0)
        {
            throw new ValidationException($"no results found for: {nameof(ctx.PoliticalBusinessId)}: {ctx.PoliticalBusinessId}");
        }

        data.Results = results;

        if (isPartialResult)
        {
            data.EndResult = PartialEndResultUtils.MergeIntoPartialEndResult(data, results);
        }

        var ccDetailsList = await _ccDetailsRepo
            .Query()
            .Include(x => x.VotingCards)
            .Where(x => x.ContestId == data.ContestId)
            .ToListAsync(ct);

        var (doiResults, notAssignableResult, aggregatedResult) = await _doiResultBuilder.BuildResults(data, ccDetailsList);

        // don't map results
        data.Results = new List<MajorityElectionResult>();

        var pdfCcDetails = _mapper.Map<List<PdfContestCountingCircleDetails>>(ccDetailsList);
        foreach (var details in pdfCcDetails)
        {
            PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(details, data.DomainOfInfluence.Type);

            // we don't need this data in the xml
            details.VotingCards = new List<PdfVotingCardResultDetail>();
        }

        var majorityElection = _mapper.Map<PdfMajorityElection>(data);
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
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(domainOfInfluence.Details, domainOfInfluence.Type);

        // we don't need this data in the xml
        domainOfInfluence.Details.VotingCards = new List<PdfVotingCardResultDetail>();
        majorityElection.DomainOfInfluence = null;

        var contest = _mapper.Map<PdfContest>(data.Contest);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
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
}
