// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionUnionDoubleProportionalResultRenderService : IRendererService
{
    private readonly DoubleProportionalResultRepo _dpResultRepo;
    private readonly TemplateService _templateService;
    private readonly IMapper _mapper;
    private readonly IClock _clock;
    private readonly DomainOfInfluenceRepo _doiRepo;

    public PdfProportionalElectionUnionDoubleProportionalResultRenderService(
        DoubleProportionalResultRepo dpResultRepo,
        TemplateService templateService,
        IMapper mapper,
        IClock clock,
        DomainOfInfluenceRepo doiRepo)
    {
        _dpResultRepo = dpResultRepo;
        _templateService = templateService;
        _mapper = mapper;
        _clock = clock;
        _doiRepo = doiRepo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var dpResult = await _dpResultRepo.GetUnionDoubleProportionalResult(ctx.PoliticalBusinessUnionId!.Value)
            ?? throw new ValidationException($"invalid data requested: {nameof(ctx.PoliticalBusinessUnionId)}: {ctx.PoliticalBusinessUnionId}");

        var pdfProportionalElectionUnion = _mapper.Map<PdfProportionalElectionUnion>(dpResult.ProportionalElectionUnion);
        pdfProportionalElectionUnion.MandateAlgorithm = dpResult.Rows.FirstOrDefault()?.ProportionalElection.MandateAlgorithm
            ?? throw new InvalidOperationException("No mandate algorithm found for union " + dpResult.ProportionalElectionUnionId);

        var domainOfInfluence = await GetDomainOfInfluence(dpResult);

        FormatResult(pdfProportionalElectionUnion.DoubleProportionalResult!);
        PreparePdfData(pdfProportionalElectionUnion.DoubleProportionalResult!);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
            ProportionalElectionUnions = new List<PdfProportionalElectionUnion> { pdfProportionalElectionUnion },
            Contest = _mapper.Map<PdfContest>(dpResult.ProportionalElectionUnion!.Contest),
            DomainOfInfluence = _mapper.Map<PdfDomainOfInfluence>(domainOfInfluence),
        };
        return await _templateService.RenderToPdf(ctx, templateBag);
    }

    private async Task<DomainOfInfluence?> GetDomainOfInfluence(DoubleProportionalResult result)
    {
        var doiIds = result.Rows.Select(r => r.ProportionalElection.DomainOfInfluenceId).ToList();

        if (doiIds.Count == 0)
        {
            return null;
        }

        return await _doiRepo.GetLowestCommonAncestorOrSelf(doiIds);
    }

    private void FormatResult(PdfDoubleProportionalResult dpResult)
    {
        // Format double proportional result, so that it will create a clean matrix of cells (UnionListCount x ProportionalElectionCount) with no gaps.
        // A cell could be "missing" if as proportional election PE1 has the list LIST01 and PE2 does not have the LIST01.
        for (var columnIndex = 0; columnIndex < dpResult.Columns.Count; columnIndex++)
        {
            var column = dpResult.Columns[columnIndex];

            for (var rowIndex = 0; rowIndex < dpResult.Rows.Count; rowIndex++)
            {
                var row = dpResult.Rows[rowIndex];

                if (row.Cells!.Any(rowCell => rowCell.List!.OrderNumber == column.UnionList!.OrderNumber && rowCell.List.ShortDescription == column.UnionList.ShortDescription))
                {
                    continue;
                }

                var newEmptyCell = new PdfDoubleProportionalResultCell
                {
                    List = new()
                    {
                        OrderNumber = column.UnionList!.OrderNumber,
                        ShortDescription = column.UnionList.ShortDescription,
                    },
                };

                column.Cells!.Insert(rowIndex, newEmptyCell);
            }
        }

        // Remove "duplicate" data, because the columns already contain all cells.
        foreach (var row in dpResult.Rows)
        {
            row.Cells = null;
        }
    }

    private void PreparePdfData(PdfDoubleProportionalResult dpResult)
    {
        foreach (var column in dpResult.Columns)
        {
            for (var rowIndex = 0; rowIndex < dpResult.Rows.Count; rowIndex++)
            {
                var electionVoteCount = dpResult.Rows[rowIndex].VoteCount;
                var cell = column.Cells![rowIndex];

                if (cell.VoteCount == 0)
                {
                    continue;
                }

                cell.VoteCountPercentageInElection = 100 * decimal.Divide(cell.VoteCount, Math.Max(1, electionVoteCount));
            }
        }
    }
}
