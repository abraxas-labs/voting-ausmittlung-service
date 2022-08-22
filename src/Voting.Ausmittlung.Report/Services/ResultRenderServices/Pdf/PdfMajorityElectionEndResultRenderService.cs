﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    private readonly IMapper _mapper;

    public PdfMajorityElectionEndResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElectionEndResult> repo,
        IMapper mapper)
    {
        _templateService = templateService;
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var data = await _repo.Query()
                       .AsSplitQuery()
                       .Include(x => x.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
                       .Include(x => x.MajorityElection.DomainOfInfluence)
                       .Include(x => x.MajorityElection.Contest.DomainOfInfluence)
                       .Include(x => x.MajorityElection.Contest.Details)
                       .Include(x => x.MajorityElection.Contest.Details!.VotingCards)
                       .Include(x => x.MajorityElection.Contest.Translations)
                       .Include(x => x.MajorityElection.Translations)
                       .FirstOrDefaultAsync(
                           x => x.MajorityElectionId == ctx.PoliticalBusinessId,
                           ct)
                   ?? throw new ValidationException(
                       $"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}");

        var majorityElection = _mapper.Map<PdfMajorityElection>(data.MajorityElection);
        PdfMajorityElectionEndResultUtil.MapCandidateEndResultsToStateLists(majorityElection.EndResult!);

        // reset the domain of influence on the result, since this is a single domain of influence report
        var domainOfInfluence = majorityElection.DomainOfInfluence;
        majorityElection.DomainOfInfluence = null;

        var contest = _mapper.Map<PdfContest>(data.MajorityElection.Contest);
        PdfContestDetailsUtil.FilterAndBuildVotingCardTotals(contest.Details!, domainOfInfluence!.Type);

        // we don't need this data in the xml
        contest.Details!.VotingCards = new List<PdfVotingCardResultDetail>();

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
            data.MajorityElection.ShortDescription);
    }
}