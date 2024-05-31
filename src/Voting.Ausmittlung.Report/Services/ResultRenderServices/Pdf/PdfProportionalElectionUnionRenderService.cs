// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionUnionRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _unionRepo;
    private readonly IMapper _mapper;
    private readonly ProportionalElectionUnionEndResultBuilder _unionEndResultBuilder;
    private readonly IClock _clock;

    public PdfProportionalElectionUnionRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionUnion> unionRepo,
        IMapper mapper,
        ProportionalElectionUnionEndResultBuilder unionEndResultBuilder,
        IClock clock)
    {
        _templateService = templateService;
        _unionRepo = unionRepo;
        _mapper = mapper;
        _unionEndResultBuilder = unionEndResultBuilder;
        _clock = clock;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var data = await _unionRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Contest.Translations)
            .Include(x => x.ProportionalElectionUnionLists)
                .ThenInclude(x => x.ProportionalElectionUnionListEntries)
                .ThenInclude(x => x.ProportionalElectionList.EndResult)
            .Include(x => x.ProportionalElectionUnionLists)
                .ThenInclude(x => x.Translations)
            .Include(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElection.EndResult!)
                    .ThenInclude(x => x.ListEndResults)
                        .ThenInclude(x => x.CandidateEndResults)
            .FirstOrDefaultAsync(x => x.Id == ctx.PoliticalBusinessUnionId, ct)
            ?? throw new ValidationException(
                $"invalid data requested: {nameof(ctx.PoliticalBusinessUnionId)}: {ctx.PoliticalBusinessUnionId}");

        var union = _mapper.Map<PdfProportionalElectionUnion>(data);
        var endResult = _unionEndResultBuilder.BuildEndResult(data);
        union.EndResult = _mapper.Map<PdfProportionalElectionUnionEndResult>(endResult);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = _mapper.Map<PdfContest>(data.Contest),
            ProportionalElectionUnions = new List<PdfProportionalElectionUnion>
                {
                    union,
                },
        };

        return await _templateService.RenderToPdf(ctx, templateBag, union.Description, PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }
}
