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
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfMajorityElectionEndResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElectionEndResult> _repo;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;
    private readonly IMapper _mapper;

    public PdfMajorityElectionEndResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElectionEndResult> repo,
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IMapper mapper)
    {
        _templateService = templateService;
        _repo = repo;
        _resultRepo = resultRepo;
        _mapper = mapper;
    }

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var data = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.MajorityElection.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.MajorityElection.DomainOfInfluence.Details!.CountOfVotersInformationSubTotals)
            .Include(x => x.MajorityElection.Contest.DomainOfInfluence)
            .Include(x => x.MajorityElection.Contest.Translations)
            .Include(x => x.MajorityElection.Translations)
            .FirstOrDefaultAsync(x => x.MajorityElectionId == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}");

        var majorityElection = _mapper.Map<PdfMajorityElection>(data.MajorityElection);
        PdfMajorityElectionEndResultUtil.MapCandidateEndResultsToStateLists(majorityElection.EndResult!);

        // reset the domain of influence on the result, since this is a single domain of influence report
        var domainOfInfluence = majorityElection.DomainOfInfluence;
        domainOfInfluence!.Details ??= new PdfContestDomainOfInfluenceDetails();
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(domainOfInfluence.Details, data.MajorityElection.DomainOfInfluence);

        // we don't need this data in the xml
        domainOfInfluence.Details.VotingCards = new List<PdfVotingCardResultDetail>();
        domainOfInfluence.Details.CountOfVotersInformationSubTotals = new List<PdfCountOfVotersInformationSubTotal>();
        majorityElection.DomainOfInfluence = null;

        if (data.TotalCountOfCountingCircles == 1)
        {
            var countingMachine = await _resultRepo.Query()
                .Where(x => x.MajorityElectionId == ctx.PoliticalBusinessId)
                .SelectMany(x => x.CountingCircle.ContestDetails)
                .Select(x => x.CountingMachine)
                .FirstAsync(ct);
            domainOfInfluence.Details.CountingMachine = countingMachine;
        }

        var contest = _mapper.Map<PdfContest>(data.MajorityElection.Contest);

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
            data.MajorityElection.ShortDescription,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }
}
