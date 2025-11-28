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

public class PdfSecondaryMajorityElectionEndResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionEndResult> _repo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResult> _resultRepo;
    private readonly IMapper _mapper;
    private readonly IClock _clock;

    public PdfSecondaryMajorityElectionEndResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, SecondaryMajorityElectionEndResult> repo,
        IDbRepository<DataContext, SecondaryMajorityElectionResult> resultRepo,
        IMapper mapper,
        IClock clock)
    {
        _templateService = templateService;
        _repo = repo;
        _resultRepo = resultRepo;
        _mapper = mapper;
        _clock = clock;
    }

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var data = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElection.PrimaryMajorityElection.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.SecondaryMajorityElection.PrimaryMajorityElection.DomainOfInfluence.Details!.CountOfVotersInformationSubTotals)
            .Include(x => x.SecondaryMajorityElection.PrimaryMajorityElection.Contest.DomainOfInfluence)
            .Include(x => x.SecondaryMajorityElection.PrimaryMajorityElection.Contest.Translations)
            .Include(x => x.SecondaryMajorityElection.Translations)
            .Include(x => x.PrimaryMajorityElectionEndResult)
            .FirstOrDefaultAsync(x => x.SecondaryMajorityElectionId == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}");

        data.MoveECountingToConventional();
        MajorityElectionResultUtils.RemoveCountToIndividualCandidatesAndAdjustTotals(data);

        var majorityElection = _mapper.Map<PdfMajorityElection>(data.SecondaryMajorityElection);
        PdfMajorityElectionEndResultUtil.MapCandidateEndResultsToStateLists(majorityElection.EndResult!);

        // reset the domain of influence on the result, since this is a single domain of influence report
        var domainOfInfluence = majorityElection.DomainOfInfluence;
        domainOfInfluence!.Details ??= new PdfContestDomainOfInfluenceDetails();
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(domainOfInfluence.Details, data.SecondaryMajorityElection.DomainOfInfluence);

        // we don't need this data in the xml
        domainOfInfluence.Details.VotingCards = new List<PdfVotingCardResultDetail>();
        domainOfInfluence.Details.CountOfVotersInformationSubTotals = new List<PdfCountOfVotersInformationSubTotal>();
        majorityElection.DomainOfInfluence = null;

        PdfCountingCircle? countingCircle = null;
        if (data.PrimaryMajorityElectionEndResult.TotalCountOfCountingCircles == 1)
        {
            var cc = await _resultRepo.Query()
                .Include(x => x.PrimaryResult.CountingCircle)
                    .ThenInclude(x => x.ResponsibleAuthority)
                .Include(x => x.PrimaryResult.CountingCircle)
                    .ThenInclude(x => x.ContactPersonDuringEvent)
                .Include(x => x.PrimaryResult.CountingCircle)
                    .ThenInclude(x => x.ContactPersonAfterEvent)
                .Include(x => x.PrimaryResult.CountingCircle)
                    .ThenInclude(x => x.ContestDetails)
                .Where(x => x.SecondaryMajorityElectionId == ctx.PoliticalBusinessId)
                .Select(x => x.PrimaryResult.CountingCircle)
                .FirstAsync(ct);
            domainOfInfluence.Details.CountingMachine = cc.ContestDetails.First().CountingMachine;
            countingCircle = _mapper.Map<PdfCountingCircle>(cc);
        }

        var contest = _mapper.Map<PdfContest>(data.SecondaryMajorityElection.Contest);

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
            CountingCircle = countingCircle,
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            data.SecondaryMajorityElection.ShortDescription,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }
}
