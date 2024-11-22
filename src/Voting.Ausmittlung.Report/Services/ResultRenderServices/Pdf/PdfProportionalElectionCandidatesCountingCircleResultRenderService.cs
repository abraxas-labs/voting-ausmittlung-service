// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionCandidatesCountingCircleResultRenderService
    : PdfProportionalElectionCountingCircleResultRenderService
{
    public PdfProportionalElectionCandidatesCountingCircleResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionResult> repo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IMapper mapper)
        : base(templateService, repo, ccDetailsRepo, mapper)
    {
    }

    protected override void PrepareAndSortData(ProportionalElectionResult data)
    {
        base.PrepareAndSortData(data);
        foreach (var listResult in data.ListResults)
        {
            listResult.CandidateResults = listResult.CandidateResults
                .OrderBy(cr => cr.Candidate.Position)
                .ToList();
        }
    }

    protected override IQueryable<ProportionalElectionResult> BuildQuery()
        => base.BuildQuery()
            .Include(x => x.ListResults)
                .ThenInclude(lr => lr.CandidateResults)
                .ThenInclude(cr => cr.Candidate.Translations)
            .Include(x => x.ListResults)
                .ThenInclude(lr => lr.CandidateResults)
                .ThenInclude(cr => cr.Candidate.ProportionalElectionList.Translations);
}
