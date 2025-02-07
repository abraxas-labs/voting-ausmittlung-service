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

public class PdfSecondaryMajorityElectionCountingCircleResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResult> _repo;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly IMapper _mapper;

    public PdfSecondaryMajorityElectionCountingCircleResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, SecondaryMajorityElectionResult> repo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IMapper mapper)
    {
        _templateService = templateService;
        _repo = repo;
        _mapper = mapper;
        _ccDetailsRepo = ccDetailsRepo;
    }

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var data = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.CandidateResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElection.Translations)
            .Include(x => x.SecondaryMajorityElection.PrimaryMajorityElection.DomainOfInfluence)
            .Include(x => x.SecondaryMajorityElection.PrimaryMajorityElection.Contest.Translations)
            .Include(x => x.SecondaryMajorityElection.PrimaryMajorityElection.Contest.DomainOfInfluence)
            .Include(x => x.PrimaryResult.CountingCircle.ResponsibleAuthority)
            .Include(x => x.PrimaryResult.CountingCircle.ContactPersonDuringEvent)
            .Include(x => x.PrimaryResult.CountingCircle.ContactPersonAfterEvent)
            .FirstOrDefaultAsync(x => x.SecondaryMajorityElectionId == ctx.PoliticalBusinessId && x.PrimaryResult.CountingCircle.BasisCountingCircleId == ctx.BasisCountingCircleId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}, countingCircleId: {ctx.BasisCountingCircleId}");

        data.CandidateResults = data.CandidateResults.OrderByDescending(c => c.VoteCount)
            .ThenBy(c => c.CandidatePosition)
            .ToList();

        var ccDetails = await _ccDetailsRepo.Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(
                x => x.ContestId == data.SecondaryMajorityElection.ContestId && x.CountingCircleId == data.PrimaryResult.CountingCircleId,
                ct);

        var majorityElection = _mapper.Map<PdfMajorityElection>(data.SecondaryMajorityElection);
        var countingCircle = majorityElection.Results![0].CountingCircle!;
        countingCircle.ContestCountingCircleDetails = _mapper.Map<PdfContestCountingCircleDetails>(ccDetails);
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(countingCircle.ContestCountingCircleDetails, data.SecondaryMajorityElection.DomainOfInfluence);

        // we don't need this data in the xml
        countingCircle.ContestCountingCircleDetails.VotingCards = new List<PdfVotingCardResultDetail>();
        countingCircle.ContestCountingCircleDetails.CountOfVotersInformationSubTotals = new List<PdfCountOfVotersInformationSubTotal>();

        // reset the counting circle on the result, since this is a single counting circle report
        majorityElection.Results![0].CountingCircle = null;

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = _mapper.Map<PdfContest>(data.SecondaryMajorityElection.Contest),
            CountingCircle = countingCircle,
            MajorityElections = new List<PdfMajorityElection>
            {
                majorityElection,
            },
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            data.SecondaryMajorityElection.ShortDescription,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }
}
