// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionDoubleProportionalResultRenderService : IRendererService
{
    private readonly DoubleProportionalResultRepo _dpResultRepo;
    private readonly TemplateService _templateService;
    private readonly IMapper _mapper;

    public PdfProportionalElectionDoubleProportionalResultRenderService(
        DoubleProportionalResultRepo dpResultRepo,
        TemplateService templateService,
        IMapper mapper)
    {
        _dpResultRepo = dpResultRepo;
        _templateService = templateService;
        _mapper = mapper;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var dpResult = await _dpResultRepo.GetElectionDoubleProportionalResult(ctx.PoliticalBusinessId)
            ?? throw new ValidationException($"invalid data requested: {nameof(ctx.PoliticalBusinessId)}: {ctx.PoliticalBusinessId}");

        var pdfProportionalElection = _mapper.Map<PdfProportionalElection>(dpResult.ProportionalElection);

        PreparePdfData(pdfProportionalElection);

        var templateBag = new PdfTemplateBag
        {
            ProportionalElections = new List<PdfProportionalElection> { pdfProportionalElection },
            Contest = _mapper.Map<PdfContest>(dpResult.ProportionalElection!.Contest),
        };
        return await _templateService.RenderToPdf(ctx, templateBag);
    }

    private void PreparePdfData(PdfProportionalElection election)
    {
        var dpResult = election.DoubleProportionalResult!;
        election.DomainOfInfluence = dpResult.Rows.Single().ProportionalElection!.DomainOfInfluence;
        dpResult.Rows = null!;

        foreach (var column in dpResult.Columns)
        {
            column.Cells = null;
        }
    }
}
