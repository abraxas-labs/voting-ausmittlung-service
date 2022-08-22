// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv;

public class CsvProportionalElectionUnionPartyVotesRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _electionUnionRepo;
    private readonly TemplateService _templateService;

    public CsvProportionalElectionUnionPartyVotesRenderService(IDbRepository<DataContext, ProportionalElectionUnion> electionUnionRepo, TemplateService templateService)
    {
        _electionUnionRepo = electionUnionRepo;
        _templateService = templateService;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        // calculations according to https://www.bfs.admin.ch/bfs/de/home/statistiken/politik/wahlen/nationalratswahlen/parteistaerken.assetdetail.5936144.htm
        // and VOTING-652
        var contestDomainOfInfluenceName = await _electionUnionRepo.Query()
                .Where(x => x.Id == ctx.PoliticalBusinessUnionId)
                .Select(x => x.Contest.DomainOfInfluence.Name)
                .FirstOrDefaultAsync(ct)
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionUnion), ctx.PoliticalBusinessUnionId!);

        var blankRowsByElectionId = await _electionUnionRepo.Query()
            .Where(x => x.Id == ctx.PoliticalBusinessUnionId)
            .SelectMany(x => x.ProportionalElectionUnionEntries)
            .Select(x => new
            {
                x.ProportionalElectionId,
                x.ProportionalElection.EndResult!.ListEndResults,
            })
            .ToDictionaryAsync(
                x => x.ProportionalElectionId,
                x => x.ListEndResults.Sum(e => e.BlankRowsCount),
                ct);

        var doiEntries = await _electionUnionRepo.Query()
            .Where(x => x.Id == ctx.PoliticalBusinessUnionId)
            .SelectMany(x => x.ProportionalElectionUnionEntries)
            .OrderBy(x => x.ProportionalElection.DomainOfInfluence.Name)
            .SelectMany(x => x.ProportionalElection.EndResult!.ListEndResults
                .SelectMany(y => y.CandidateEndResults)
                .GroupBy(y => y.Candidate.Party!.Translations.Single().ShortDescription)// there should only be one matching translation due to the language query filter
                .Select(y => new PartyVotesCsvEntry(
                    x.ProportionalElectionId,
                    x.ProportionalElection.DomainOfInfluence.Name,
                    y.Key,
                    y.Sum(z => z.VoteCount),
                    x.ProportionalElection.EndResult.CountOfVoters.TotalAccountedBallots)))
            .ToListAsync(ct);

        var totalFictivePartyVoters = 0M;
        var totalSubmittedVotes = 0;
        foreach (var politicalBusinessEntries in doiEntries.GroupBy(x => x.PoliticalBusinessId))
        {
            var emptyVotes = blankRowsByElectionId.GetValueOrDefault(politicalBusinessEntries.Key!.Value);
            var politicalBusinessSubmittedVotes = politicalBusinessEntries.Sum(x => x.PartyVotes) + emptyVotes;
            totalSubmittedVotes += politicalBusinessSubmittedVotes;

            foreach (var politicalBusinessEntry in politicalBusinessEntries)
            {
                politicalBusinessEntry.EmptyVotes = emptyVotes;
                politicalBusinessEntry.PoliticalBusinessSubmittedVotes = politicalBusinessSubmittedVotes;
                politicalBusinessEntry.FictivePartyVoters = politicalBusinessEntry.PartyVotes * (politicalBusinessEntry.AccountedBallots / Math.Max(1, (decimal)politicalBusinessEntry.PoliticalBusinessSubmittedVotes));
                politicalBusinessEntry.PartyStrength = 100 * politicalBusinessEntry.PartyVotes / Math.Max(1, (decimal)politicalBusinessEntry.PoliticalBusinessSubmittedVotes);
                totalFictivePartyVoters += politicalBusinessEntry.FictivePartyVoters;
            }
        }

        totalFictivePartyVoters = Math.Max(1, totalFictivePartyVoters);
        var contestEntries = new List<PartyVotesCsvEntry>();
        foreach (var partyEntries in doiEntries.GroupBy(x => x.PartyShortDescription))
        {
            var contestEntry = new PartyVotesCsvEntry(
                null,
                contestDomainOfInfluenceName,
                partyEntries.Key,
                partyEntries.Sum(x => x.PartyVotes),
                partyEntries.Sum(x => x.AccountedBallots));
            contestEntry.PoliticalBusinessSubmittedVotes = totalSubmittedVotes;
            contestEntry.EmptyVotes = blankRowsByElectionId.Values.Sum();
            contestEntry.FictivePartyVoters = partyEntries.Sum(x => x.FictivePartyVoters);
            contestEntry.PartyStrength = 100 * contestEntry.FictivePartyVoters / totalFictivePartyVoters;
            contestEntries.Add(contestEntry);
        }

        var allEntries = contestEntries
            .OrderByDescending(x => x.PartyStrength)
            .Concat(doiEntries.OrderByDescending(x => x.DomainOfInfluenceName).ThenBy(x => x.PartyStrength));
        return await Task.FromResult(_templateService.RenderToCsv(ctx, allEntries));
    }

    private class PartyVotesCsvEntry
    {
        public PartyVotesCsvEntry(Guid? politicalBusinessId, string domainOfInfluenceName, string partyShortDescription, int partyVotes, int accountedBallots)
        {
            PoliticalBusinessId = politicalBusinessId;
            DomainOfInfluenceName = domainOfInfluenceName;
            PartyShortDescription = partyShortDescription;
            PartyVotes = partyVotes;
            AccountedBallots = accountedBallots;
        }

        [Ignore]
        public Guid? PoliticalBusinessId { get; set; }

        [Name("Wahlkreis")]
        public string DomainOfInfluenceName { get; }

        [Name("Partei")]
        public string PartyShortDescription { get; }

        [Name("Parteistärke")]
        [Format("N2")]
        public decimal PartyStrength { get; set; }

        [Name("Fiktive Wählende (Partei)")]
        [Format("N2")]
        public decimal FictivePartyVoters { get; set; }

        [Name("Gültige Wahlzettel (aller Wahlkreise)")]
        public int AccountedBallots { get; }

        [Name("Total abgegebene Stimmen (aller Wahlkreise)")]
        public int PoliticalBusinessSubmittedVotes { get; set; }

        [Name("Parteistimmen")]
        public int PartyVotes { get; }

        [Name("Leere Stimmen")]
        public int EmptyVotes { get; set; }
    }
}
