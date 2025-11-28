// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionCandidatesEndResultRenderService
    : PdfProportionalElectionEndResultRenderService
{
    public PdfProportionalElectionCandidatesEndResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionEndResult> repo,
        IMapper mapper,
        IClock clock)
        : base(templateService, repo, mapper, clock)
    {
    }

    protected override void PrepareAndSortData(ReportRenderContext ctx, ProportionalElectionEndResult data)
    {
        base.PrepareAndSortData(ctx, data);

        if (ctx.Template.Key == AusmittlungPdfProportionalElectionTemplates.ListCandidateVotesEndResults.Key
            && data.ProportionalElection.DomainOfInfluence.Canton == DomainOfInfluenceCanton.Zh)
        {
            foreach (var listEndResult in data.ListEndResults)
            {
                listEndResult.CandidateEndResults = listEndResult.CandidateEndResults
                    .OrderBy(cr => cr.Candidate.Number)
                    .ToList();
            }

            return;
        }

        foreach (var listResult in data.ListEndResults)
        {
            listResult.CandidateEndResults = listResult.CandidateEndResults
                .OrderBy(cr => cr.Rank)
                .ThenBy(cr => cr.Candidate.Position)
                .ToList();
        }
    }

    protected override IQueryable<ProportionalElectionEndResult> BuildQuery()
        => base.BuildQuery()
            .Include(x => x.ListEndResults)
                .ThenInclude(lr => lr.CandidateEndResults)
                .ThenInclude(cr => cr.Candidate.Translations)
            .Include(x => x.ListEndResults)
                .ThenInclude(lr => lr.CandidateEndResults)
                .ThenInclude(cr => cr.Candidate.ProportionalElectionList);
}
