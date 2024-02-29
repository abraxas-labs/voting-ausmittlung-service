// (c) Copyright 2024 by Abraxas Informatik AG
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

public class PdfProportionalElectionCandidateVoteSourcesEndResultRenderService
    : PdfProportionalElectionCandidatesEndResultRenderService
{
    public PdfProportionalElectionCandidateVoteSourcesEndResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionEndResult> repo,
        IMapper mapper,
        IClock clock)
        : base(templateService, repo, mapper, clock)
    {
    }

    protected override void PrepareAndSortData(ProportionalElectionEndResult data)
    {
        base.PrepareAndSortData(data);
        foreach (var listEndResult in data.ListEndResults)
        {
            foreach (var candidateEndResult in listEndResult.CandidateEndResults)
            {
                AddMissingVoteSourcesAndSort(candidateEndResult, data.ListEndResults);
            }
        }
    }

    protected override IQueryable<ProportionalElectionEndResult> BuildQuery()
    {
        return base.BuildQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .ThenInclude(x => x.VoteSources)
            .ThenInclude(x => x.List!.Translations);
    }

    protected override void PreparePdfData(PdfTemplateBag templateBag)
    {
        foreach (var election in templateBag.ProportionalElections!)
        {
            PdfProportionalElectionListResultVoteSourceBuilder.BuildVoteSourceSums(election.EndResult!.ListEndResults);
        }
    }

    private void AddMissingVoteSourcesAndSort(
        ProportionalElectionCandidateEndResult candidateResult,
        IEnumerable<ProportionalElectionListEndResult> allListResults)
    {
        var listVoteSources = candidateResult.VoteSources
            .ToDictionary(x => x.ListId ?? Guid.Empty);
        var missingListVoteSources = allListResults
            .Where(x => !listVoteSources.ContainsKey(x.ListId))
            .Select(x => new ProportionalElectionCandidateVoteSourceEndResult
            {
                List = x.List,
                ListId = x.ListId,
                CandidateResult = candidateResult,
                CandidateResultId = candidateResult.Id,
            })
            .ToList();

        if (!listVoteSources.ContainsKey(Guid.Empty))
        {
            missingListVoteSources.Add(new ProportionalElectionCandidateVoteSourceEndResult
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
