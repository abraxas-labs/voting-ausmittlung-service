// (c) Copyright 2024 by Abraxas Informatik AG
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
                    x.ProportionalElection.EndResult.CountOfVoters.TotalAccountedBallots,
                    x.ProportionalElection.EndResult.CountOfVoters.TotalBlankBallots)))
            .ToListAsync(ct);

        var totalFictivePartyVoters = 0M;
        foreach (var politicalBusinessEntries in doiEntries.GroupBy(x => x.PoliticalBusinessId))
        {
            var politicalBusinessSubmittedVotes = politicalBusinessEntries.Sum(x => x.PartyVotes);
            var totalFictivePartyVotersOfPoliticalBusiness = 0M;
            foreach (var politicalBusinessEntry in politicalBusinessEntries)
            {
                politicalBusinessEntry.PoliticalBusinessSubmittedVotes = politicalBusinessSubmittedVotes + politicalBusinessEntry.BlankBallots;
                politicalBusinessEntry.FictivePartyVoters = politicalBusinessEntry.PartyVotes * (politicalBusinessEntry.AccountedBallots / Math.Max(1, (decimal)politicalBusinessEntry.PoliticalBusinessSubmittedVotes));
                totalFictivePartyVotersOfPoliticalBusiness += politicalBusinessEntry.FictivePartyVoters;
            }

            totalFictivePartyVoters += totalFictivePartyVotersOfPoliticalBusiness;
            foreach (var politicalBusinessEntry in politicalBusinessEntries)
            {
                politicalBusinessEntry.PartyStrength = 100 * politicalBusinessEntry.FictivePartyVoters / Math.Max(1, totalFictivePartyVotersOfPoliticalBusiness);
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
                partyEntries.Sum(x => x.AccountedBallots),
                partyEntries.Sum(x => x.BlankBallots));
            contestEntry.PoliticalBusinessSubmittedVotes = partyEntries.Sum(x => x.PoliticalBusinessSubmittedVotes);
            contestEntry.FictivePartyVoters = partyEntries.Sum(x => x.FictivePartyVoters);
            contestEntry.PartyStrength = 100 * contestEntry.FictivePartyVoters / totalFictivePartyVoters;
            contestEntries.Add(contestEntry);
        }

        var allEntries = contestEntries
            .OrderByDescending(x => x.PartyStrength).ThenBy(x => x.PartyShortDescription)
            .Concat(doiEntries.OrderBy(x => x.DomainOfInfluenceName).ThenByDescending(x => x.PartyStrength).ThenBy(x => x.PartyShortDescription));
        return await Task.FromResult(_templateService.RenderToCsv(ctx, allEntries));
    }

    private class PartyVotesCsvEntry
    {
        public PartyVotesCsvEntry(Guid? politicalBusinessId, string domainOfInfluenceName, string partyShortDescription, int partyVotes, int accountedBallots, int blankBallots)
        {
            PoliticalBusinessId = politicalBusinessId;
            DomainOfInfluenceName = domainOfInfluenceName;
            PartyShortDescription = partyShortDescription;
            PartyVotes = partyVotes;
            AccountedBallots = accountedBallots;
            BlankBallots = blankBallots;
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

        [Name("Gültige Wahlzettel")]
        public int AccountedBallots { get; }

        [Name("Total abgegebene Stimmen")]
        public int PoliticalBusinessSubmittedVotes { get; set; }

        [Name("Parteistimmen")]
        public int PartyVotes { get; }

        [Name("Leere Stimmen")]
        public int BlankBallots { get; set; }
    }
}
