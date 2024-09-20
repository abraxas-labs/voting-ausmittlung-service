// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

public abstract class WabstiCWPBaseRenderService : IRendererService
{
    private const string FileNameParamReplacement = "_";
    private static readonly Regex _validFileNameParam = new("[. ]+", RegexOptions.Compiled);

    private readonly TemplateService _templateService;

    protected WabstiCWPBaseRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElection> repo)
    {
        Repo = repo;
        _templateService = templateService;
    }

    protected IDbRepository<DataContext, ProportionalElection> Repo { get; }

    public abstract Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default);

    protected async Task<FileModel> RenderToCsv<TRow>(
        ReportRenderContext ctx,
        IEnumerable<TRow> records,
        Action<IWriter>? configure = null,
        params string[] filenameArgs)
    {
        if (ctx.Template.ResultType == ResultType.PoliticalBusinessResult)
        {
            filenameArgs = filenameArgs.Concat(new[] { await LoadAdditionalFileNameArg(ctx) }).ToArray();
        }

        return _templateService.RenderToCsv(
            ctx,
            records,
            configure,
            filenameArgs);
    }

    protected async Task<FileModel> RenderToCsv<TRow>(
        ReportRenderContext ctx,
        IAsyncEnumerable<TRow> records,
        params string[] filenameArgs)
    {
        if (ctx.Template.ResultType == ResultType.PoliticalBusinessResult)
        {
            filenameArgs = filenameArgs.Concat(new[] { await LoadAdditionalFileNameArg(ctx) }).ToArray();
        }

        return _templateService.RenderToCsv(
            ctx,
            records,
            filenameArgs);
    }

    private async Task<string> LoadAdditionalFileNameArg(ReportRenderContext ctx)
    {
        var domainOfInfluenceShortName = await Repo.Query()
            .Where(pe => pe.Id == ctx.PoliticalBusinessId)
            .Select(pe => pe.DomainOfInfluence.ShortName)
            .FirstOrDefaultAsync() ?? throw new EntityNotFoundException(nameof(ProportionalElection), ctx.PoliticalBusinessId);

        return _validFileNameParam.Replace(domainOfInfluenceShortName, FileNameParamReplacement);
    }
}
