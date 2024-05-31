// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfMajorityElectionEndResultDetailRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _repo;
    private readonly IMapper _mapper;
    private readonly MajorityElectionDomainOfInfluenceResultBuilder _doiResultBuilder;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly IClock _clock;

    public PdfMajorityElectionEndResultDetailRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElection> repo,
        IMapper mapper,
        MajorityElectionDomainOfInfluenceResultBuilder doiResultBuilder,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IClock clock)
    {
        _templateService = templateService;
        _repo = repo;
        _mapper = mapper;
        _doiResultBuilder = doiResultBuilder;
        _ccDetailsRepo = ccDetailsRepo;
        _clock = clock;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var data = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.EndResult!.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.Results).ThenInclude(x => x.CandidateResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.Results).ThenInclude(x => x.CountingCircle)
            .Include(x => x.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.Contest.DomainOfInfluence)
            .Include(x => x.Contest.Translations)
            .Include(x => x.Contest.CantonDefaults)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}");

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
