// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionEndResultListUnionsRenderService
    : PdfProportionalElectionEndResultCalculationRenderService
{
    public PdfProportionalElectionEndResultListUnionsRenderService(
        IDbRepository<DataContext, ProportionalElectionEndResult> repo,
        IMapper mapper,
        TemplateService templateService,
        IClock clock)
        : base(repo, mapper, templateService, clock)
    {
        IncludeCalculationRounds = false;
    }

    protected override IQueryable<ProportionalElectionEndResult> BuildQuery()
    {
        return base.BuildQuery()
            .Include(x => x.ProportionalElection.Contest.Details!.VotingCards)
            .Include(x => x.ProportionalElection.Contest.Details!.CountOfVotersInformationSubTotals);
    }

    protected override void SortData(ProportionalElectionEndResult data)
    {
        base.SortData(data);
        data.ProportionalElection.Contest.Details?.OrderVotingCardsAndSubTotals();
    }

    protected override void MapAdditionalElectionData(ProportionalElectionEndResult endResult, PdfProportionalElection pdfElection)
    {
        if (endResult.HagenbachBischoffRootGroup == null)
        {
            return;
        }

        var listGroups = endResult.HagenbachBischoffRootGroup
            .AllGroups
            .Where(x => x.Type == HagenbachBischoffGroupType.List)
            .OrderBy(x => x.List!.Position)
            .ToList();

        pdfElection.EndResult!.ListUnionEndResult = BuildListUnions(
            endResult.HagenbachBischoffRootGroup,
            listGroups,
            HagenbachBischoffGroupType.ListUnion);
        pdfElection.EndResult.SubListUnionEndResult = BuildListUnions(
            endResult.HagenbachBischoffRootGroup,
            listGroups,
            HagenbachBischoffGroupType.SubListUnion);

        // remove unneeded values
        pdfElection.EndResult.Calculation!.HagenbachBischoffRootGroup!.InitialDistributionValues =
            new List<PdfHagenbachBischoffInitialGroupValues>();
        pdfElection.EndResult.Calculation.HagenbachBischoffListUnionGroups =
            new List<PdfHagenbachBischoffGroup>();
        pdfElection.EndResult.Calculation.HagenbachBischoffSubListUnionGroups =
            new List<PdfHagenbachBischoffGroup>();
    }

    private PdfProportionalElectionListUnionEndResult BuildListUnions(
        HagenbachBischoffGroup rootGroup,
        List<HagenbachBischoffGroup> listGroups,
        HagenbachBischoffGroupType type)
    {
        var unionGroups = rootGroup
            .AllGroups
            .Where(x => x.Type == type)
            .ToList();

        var listIdsByGroupId = unionGroups.ToDictionary(
            x => x.Id,
            x => x.AllLists.Select(y => y.Id).ToList());

        var result = new PdfProportionalElectionListUnionEndResult
        {
            Groups = Mapper.Map<List<PdfHagenbachBischoffGroup>>(unionGroups),
            Entries = Mapper.Map<List<PdfProportionalElectionListUnionEndResultEntry>>(listGroups),
        };

        foreach (var entry in result.Entries)
        {
            entry.GroupVoteCounts = unionGroups
                .Select(x => listIdsByGroupId[x.Id].Contains(entry.List!.Id) ? entry.VoteCount : 0)
                .ToList();
        }

        // rm unneeded data
        foreach (var group in result.Groups)
        {
            group.InitialDistributionValues = new List<PdfHagenbachBischoffInitialGroupValues>();
        }

        return result;
    }
}
