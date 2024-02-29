// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv;

public class CsvProportionalElectionUnionVoterParticipationRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _electionUnionRepo;
    private readonly TemplateService _templateService;

    public CsvProportionalElectionUnionVoterParticipationRenderService(IDbRepository<DataContext, ProportionalElectionUnion> electionUnionRepo, TemplateService templateService)
    {
        _electionUnionRepo = electionUnionRepo;
        _templateService = templateService;
    }

    public Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var contestEntries = _electionUnionRepo.Query()
            .Where(x => x.Id == ctx.PoliticalBusinessUnionId)
            .Select(x => new VoterParticipationCsvEntry(
                x.Contest.DomainOfInfluence.Name,
                x.ProportionalElectionUnionEntries.Sum(y => y.ProportionalElection.EndResult!.TotalCountOfVoters),
                x.ProportionalElectionUnionEntries.Sum(y => y.ProportionalElection.EndResult!.CountOfVoters.TotalReceivedBallots)))
            .AsAsyncEnumerable();

        var domainOfInfluenceEntries = _electionUnionRepo.Query()
            .Where(x => x.Id == ctx.PoliticalBusinessUnionId)
            .SelectMany(x => x.ProportionalElectionUnionEntries)
            .OrderBy(x => x.ProportionalElection.DomainOfInfluence.Name)
            .Select(x => new VoterParticipationCsvEntry(
                x.ProportionalElection.DomainOfInfluence.Name,
                x.ProportionalElection.EndResult!.TotalCountOfVoters,
                x.ProportionalElection.EndResult!.CountOfVoters.TotalReceivedBallots))
            .AsAsyncEnumerable();

        var allEntries = contestEntries.Concat(domainOfInfluenceEntries);
        return Task.FromResult(_templateService.RenderToCsv(ctx, allEntries));
    }

    private class VoterParticipationCsvEntry
    {
        public VoterParticipationCsvEntry(string domainOfInfluenceName, int countOfVoters, int receivedBallots)
        {
            DomainOfInfluenceName = domainOfInfluenceName;
            CountOfVoters = countOfVoters;
            ReceivedBallots = receivedBallots;
        }

        [Name("Wahlkreis")]
        public string DomainOfInfluenceName { get; }

        [Name("Stimmberechtigte")]
        public int CountOfVoters { get; }

        [Name("Eingegangene Wahlzettel")]
        public int ReceivedBallots { get; }

        [Name("Wahlbeteiligung")]
        [Format("N2")]
        public decimal VoterParticipation => 100 * ReceivedBallots / Math.Max((decimal)CountOfVoters, 1);
    }
}
