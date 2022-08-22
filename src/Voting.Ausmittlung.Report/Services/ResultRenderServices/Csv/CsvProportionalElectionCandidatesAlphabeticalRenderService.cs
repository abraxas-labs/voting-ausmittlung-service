// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv;

public class CsvProportionalElectionCandidatesAlphabeticalRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElectionCandidate> _candidatesRepo;

    public CsvProportionalElectionCandidatesAlphabeticalRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionCandidate> candidatesRepo)
    {
        _templateService = templateService;
        _candidatesRepo = candidatesRepo;
    }

    public Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var candidates = _candidatesRepo.Query()
            .Where(c => c.ProportionalElectionList.ProportionalElectionId == ctx.PoliticalBusinessId)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new
            {
                Listennummer = c.ProportionalElectionList.OrderNumber,
                c.Position,
                Nachname = c.LastName,
                Vorname = c.FirstName,
                Wohnort = c.Locality,
                Jahrgang = c.DateOfBirth.Year,
            })
            .AsAsyncEnumerable();

        return Task.FromResult(_templateService.RenderToCsv(
            ctx,
            candidates));
    }
}
