// (c) Copyright 2022 by Abraxas Informatik AG
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

public class PdfMajorityElectionCountingCircleResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _repo;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly IMapper _mapper;

    public PdfMajorityElectionCountingCircleResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElectionResult> repo,
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
                       .Include(x => x.MajorityElection.Translations)
                       .Include(x => x.MajorityElection.DomainOfInfluence)
                       .Include(x => x.MajorityElection.Contest.Translations)
                       .Include(x => x.MajorityElection.Contest.DomainOfInfluence)
                       .Include(x => x.CountingCircle.ResponsibleAuthority)
                       .Include(x => x.CountingCircle.ContactPersonDuringEvent)
                       .Include(x => x.CountingCircle.ContactPersonAfterEvent)
                       .FirstOrDefaultAsync(
                           x =>
                               x.MajorityElectionId == ctx.PoliticalBusinessId &&
                               x.CountingCircle.BasisCountingCircleId == ctx.BasisCountingCircleId,
                           ct)
                   ?? throw new ValidationException(
                       $"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}, countingCircleId: {ctx.BasisCountingCircleId}");

        data.CandidateResults = data.CandidateResults.OrderBy(c => c.VoteCount)
            .ThenBy(c => c.CandidatePosition)
            .ToList();

        var ccDetails = await _ccDetailsRepo.Query()
            .Include(x => x.VotingCards)
            .FirstOrDefaultAsync(
                x => x.ContestId == data.MajorityElection.ContestId && x.CountingCircleId == data.CountingCircleId,
                ct);

        var majorityElection = _mapper.Map<PdfMajorityElection>(data.MajorityElection);
        var countingCircle = majorityElection.Results![0].CountingCircle!;
        countingCircle.ContestCountingCircleDetails = _mapper.Map<PdfContestCountingCircleDetails>(ccDetails);
        PdfContestCountingCircleDetailsUtil.FilterAndBuildVotingCardTotals(countingCircle.ContestCountingCircleDetails, majorityElection.DomainOfInfluence!.Type);

        // we don't need this data in the xml
        countingCircle.ContestCountingCircleDetails.VotingCards = new List<PdfVotingCardResultDetail>();

        // reset the counting circle on the result, since this is a single counting circle report
        majorityElection.Results![0].CountingCircle = null;

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = _mapper.Map<PdfContest>(data.MajorityElection.Contest),
            CountingCircle = countingCircle,
            MajorityElections = new List<PdfMajorityElection>
                {
                    majorityElection,
                },
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            data.MajorityElection.ShortDescription);
    }
}
