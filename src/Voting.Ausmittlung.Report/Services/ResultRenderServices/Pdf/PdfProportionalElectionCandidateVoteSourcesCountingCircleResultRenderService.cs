// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionCandidateVoteSourcesCountingCircleResultRenderService
: PdfProportionalElectionCandidatesCountingCircleResultRenderService
{
    public PdfProportionalElectionCandidateVoteSourcesCountingCircleResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionResult> repo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IMapper mapper,
        IClock clock)
        : base(templateService, repo, ccDetailsRepo, mapper, clock)
    {
    }

    protected override void PrepareAndSortData(ProportionalElectionResult data)
    {
        base.PrepareAndSortData(data);
        foreach (var listResult in data.ListResults)
        {
            foreach (var candidateResult in listResult.CandidateResults)
            {
                AddMissingVoteSourcesAndSort(candidateResult, data.ListResults);
            }
        }
    }

    protected override IQueryable<ProportionalElectionResult> BuildQuery()
    {
        return base.BuildQuery()
            .Include(x => x.ListResults)
            .ThenInclude(x => x.CandidateResults)
            .ThenInclude(x => x.VoteSources)
            .ThenInclude(x => x.List!.Translations);
    }

    protected override void PreparePdfData(PdfTemplateBag templateBag)
    {
        foreach (var election in templateBag.ProportionalElections!)
        {
            PdfProportionalElectionListResultVoteSourceBuilder.BuildVoteSourceSums(election.Results!.SelectMany(r => r.ListResults));
        }
    }

    private void AddMissingVoteSourcesAndSort(
        ProportionalElectionCandidateResult candidateResult,
        IEnumerable<ProportionalElectionListResult> allListResults)
    {
        var listVoteSources = candidateResult.VoteSources
            .ToDictionary(x => x.ListId ?? Guid.Empty);

        var missingListVoteSources = allListResults
            .Where(x => !listVoteSources.ContainsKey(x.ListId))
            .Select(x => new ProportionalElectionCandidateVoteSourceResult
            {
                List = x.List,
                ListId = x.ListId,
                CandidateResult = candidateResult,
                CandidateResultId = candidateResult.Id,
            })
            .ToList();

        if (!listVoteSources.ContainsKey(Guid.Empty))
        {
            missingListVoteSources.Add(new ProportionalElectionCandidateVoteSourceResult
            {
                CandidateResult = candidateResult,
                CandidateResultId = candidateResult.Id,
            });
        }

        candidateResult.VoteSources = listVoteSources.Values.Concat(missingListVoteSources)
            .OrderBy(x => x.List == null)
            .ThenBy(x => x.List?.Position)
            .ToList();
    }
}
