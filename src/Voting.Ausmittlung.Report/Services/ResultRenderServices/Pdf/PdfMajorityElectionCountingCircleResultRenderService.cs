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
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfMajorityElectionCountingCircleResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _repo;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly DomainOfInfluenceRepo _doiRepo;
    private readonly IMapper _mapper;
    private readonly IClock _clock;
    private readonly PdfMajorityElectionUtil _pdfMajorityElectionUtil;

    public PdfMajorityElectionCountingCircleResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElectionResult> repo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        DomainOfInfluenceRepo doiRepo,
        IMapper mapper,
        IClock clock,
        PdfMajorityElectionUtil pdfMajorityElectionUtil)
    {
        _templateService = templateService;
        _repo = repo;
        _mapper = mapper;
        _clock = clock;
        _pdfMajorityElectionUtil = pdfMajorityElectionUtil;
        _ccDetailsRepo = ccDetailsRepo;
        _doiRepo = doiRepo;
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
            .FirstOrDefaultAsync(x => x.MajorityElectionId == ctx.PoliticalBusinessId && x.CountingCircle.BasisCountingCircleId == ctx.BasisCountingCircleId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}, countingCircleId: {ctx.BasisCountingCircleId}");

        data.MoveECountingToConventional();

        data.CandidateResults = data.CandidateResults.OrderByDescending(c => c.VoteCount)
            .ThenBy(c => c.CandidatePosition)
            .ToList();

        var ccDetails = await _ccDetailsRepo.Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(
                x => x.ContestId == data.MajorityElection.ContestId && x.CountingCircleId == data.CountingCircleId,
                ct);

        var majorityElection = _mapper.Map<PdfMajorityElection>(data.MajorityElection);
        var countingCircle = majorityElection.Results![0].CountingCircle!;
        countingCircle.ContestCountingCircleDetails = _mapper.Map<PdfContestCountingCircleDetails>(ccDetails);
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(countingCircle.ContestCountingCircleDetails, data.MajorityElection.DomainOfInfluence);
        await _pdfMajorityElectionUtil.FillEmptyVoteCountDisabled(majorityElection);

        // we don't need this data in the xml
        countingCircle.ContestCountingCircleDetails.VotingCards = new List<PdfVotingCardResultDetail>();
        countingCircle.ContestCountingCircleDetails.CountOfVotersInformationSubTotals = new List<PdfCountOfVotersInformationSubTotal>();

        // reset the counting circle on the result, since this is a single counting circle report
        majorityElection.Results![0].CountingCircle = null;

        // Set the name of the domain of influence that matches the reporting level and is in the same hierarchy as the counting circle
        var reportLevel = data.MajorityElection.ReportDomainOfInfluenceLevel;
        var relevantDois = await _doiRepo.GetRelevantDomainOfInfluencesForReportingLevel(
            data.MajorityElection.DomainOfInfluenceId,
            reportLevel);
        var relevantDoiForCc = relevantDois
            .Where(d => d.DomainOfInfluence!.CountingCircles.Any(doiCc => doiCc.CountingCircleId == data.CountingCircleId))
            .OrderByDescending(x => x.ReportLevel) // In theory, there could be multiple DOIs (ex. with HideLowerDoi flag). We prefer the lowest one that matches
            .FirstOrDefault();
        majorityElection.ReportingLevelName = relevantDoiForCc?.DomainOfInfluence?.NameForProtocol ?? string.Empty;

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
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
            data.MajorityElection.ShortDescription,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }
}
