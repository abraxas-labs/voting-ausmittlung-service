// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

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

public class CsvProportionalElectionUnionPartyMandatesRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _electionUnionRepo;
    private readonly TemplateService _templateService;

    public CsvProportionalElectionUnionPartyMandatesRenderService(IDbRepository<DataContext, ProportionalElectionUnion> electionUnionRepo, TemplateService templateService)
    {
        _electionUnionRepo = electionUnionRepo;
        _templateService = templateService;
    }

    public Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        // ef core can't translate the order by after the group by in an inner query
        // therefore this needs to be done clientside and prevents streaming the csv entries
        // should not matter too much here, since only very limited data is expected.
        var contestEntries = _electionUnionRepo.Query()
            .Where(x => x.Id == ctx.PoliticalBusinessUnionId)
            .SelectMany(x => x.ProportionalElectionUnionEntries
                .SelectMany(y => y.ProportionalElection.EndResult!.ListEndResults)
                .SelectMany(y => y.CandidateEndResults)
                .Where(y => y.State == ProportionalElectionCandidateEndResultState.Elected)
                .GroupBy(y => y.Candidate.Party!.Translations.Single().ShortDescription)// there should only be one matching translation due to the language query filter
                .Select(y => new PartyMandatesCsvEntry(x.Contest.DomainOfInfluence.Name, y.Key, y.Count())))
            .AsAsyncEnumerable()
            .OrderByDescending(x => x.NumberOfMandates);

        var domainOfInfluenceEntries = _electionUnionRepo.Query()
            .Where(x => x.Id == ctx.PoliticalBusinessUnionId)
            .SelectMany(x => x.ProportionalElectionUnionEntries)
            .SelectMany(x => x.ProportionalElection.EndResult!.ListEndResults
                .SelectMany(y => y.CandidateEndResults)
                .Where(y => y.State == ProportionalElectionCandidateEndResultState.Elected)
                .GroupBy(y => y.Candidate.Party!.Translations.Single().ShortDescription)// there should only be one matching translation due to the language query filter
                .Select(y => new PartyMandatesCsvEntry(x.ProportionalElection.DomainOfInfluence.Name, y.Key, y.Count())))
            .AsAsyncEnumerable()
            .OrderBy(x => x.DomainOfInfluenceName)
            .ThenByDescending(x => x.NumberOfMandates);

        var allEntries = contestEntries.Concat(domainOfInfluenceEntries);
        return Task.FromResult(_templateService.RenderToCsv(ctx, allEntries));
    }

    private class PartyMandatesCsvEntry
    {
        public PartyMandatesCsvEntry(string domainOfInfluenceName, string partyShortDescription, int numberOfMandates)
        {
            DomainOfInfluenceName = domainOfInfluenceName;
            PartyShortDescription = partyShortDescription;
            NumberOfMandates = numberOfMandates;
        }

        [Name("Wahlkreis")]
        public string DomainOfInfluenceName { get; }

        [Name("Partei")]
        public string PartyShortDescription { get; }

        [Name("Anzahl Sitze")]
        public int NumberOfMandates { get; }
    }
}
