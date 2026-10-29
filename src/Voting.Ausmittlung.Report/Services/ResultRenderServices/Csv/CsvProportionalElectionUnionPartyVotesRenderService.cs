// (c) Copyright by Abraxas Informatik AG
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

        var endResultEntries = await _electionUnionRepo.Query()
            .AsSplitQuery()
            .Where(x => x.Id == ctx.PoliticalBusinessUnionId)
            .SelectMany(x => x.ProportionalElectionUnionEntries)
            .OrderBy(x => x.ProportionalElection.DomainOfInfluence.Name)
            .Select(x => x.ProportionalElection.EndResult!)
            .Select(x => new EndResultVotesEntry
            {
                PoliticalBusinessId = x.ProportionalElectionId,
                NumberOfMandates = x.ProportionalElection.NumberOfMandates,
                AccountedBallots = x.CountOfVoters.TotalAccountedBallots,
                BlankRowsOnListsWithoutParty = x.TotalCountOfBlankRowsOnListsWithoutParty,
                DomainOfInfluenceName = x.ProportionalElection.DomainOfInfluence.Name,
                ListVotesEntries = x.ListEndResults.Select(y => new ListVotesEntry(
                    y.BlankRowsCount,
                    y.CandidateEndResults.Select(c => new CandidateVotesEntry(c.VoteCount, c.Candidate.Party!.Translations.Single().ShortDescription)).ToList()))
                    .ToList(),
            })
            .ToListAsync(ct);

        var partyVotes = endResultEntries
            .SelectMany(BuildPartyVotes)
            .ToList();

        var totalFictivePartyVoters = 0M;
        foreach (var politicalBusinessEntries in partyVotes.GroupBy(x => x.PoliticalBusinessId))
        {
            var politicalBusinessSubmittedVotes = politicalBusinessEntries.Sum(x => x.PartyVotes);
            var totalFictivePartyVotersOfPoliticalBusiness = 0M;
            foreach (var politicalBusinessEntry in politicalBusinessEntries)
            {
                politicalBusinessEntry.PoliticalBusinessSubmittedVotes = politicalBusinessSubmittedVotes + politicalBusinessEntry.BlankRowsOnListsWithoutParty;
                var partyVotePercentage = politicalBusinessEntry.PartyVotes / (decimal)Math.Max(1, politicalBusinessEntry.PoliticalBusinessSubmittedVotes);
                var shareOfBlankRows = partyVotePercentage * politicalBusinessEntry.BlankRowsOnListsWithoutParty;
                politicalBusinessEntry.FictivePartyVoters = (politicalBusinessEntry.PartyVotes + shareOfBlankRows) / Math.Max(1, politicalBusinessEntry.NumberOfMandates);
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
        foreach (var partyEntries in partyVotes.GroupBy(x => x.PartyShortDescription))
        {
            var contestEntry = new PartyVotesCsvEntry(
                null,
                0,
                contestDomainOfInfluenceName,
                partyEntries.Key,
                partyEntries.Sum(x => x.PartyVotes),
                partyEntries.Sum(x => x.AccountedBallots),
                partyEntries.Sum(x => x.BlankRowsOnListsWithoutParty));
            contestEntry.PoliticalBusinessSubmittedVotes = partyEntries.Sum(x => x.PoliticalBusinessSubmittedVotes);
            contestEntry.FictivePartyVoters = partyEntries.Sum(x => x.FictivePartyVoters);
            contestEntry.PartyStrength = 100 * contestEntry.FictivePartyVoters / totalFictivePartyVoters;
            contestEntries.Add(contestEntry);
        }

        var allEntries = contestEntries
            .OrderByDescending(x => x.PartyStrength).ThenBy(x => x.PartyShortDescription)
            .Concat(partyVotes.OrderBy(x => x.DomainOfInfluenceName).ThenByDescending(x => x.PartyStrength).ThenBy(x => x.PartyShortDescription));
        return await Task.FromResult(_templateService.RenderToCsv(ctx, allEntries));
    }

    private IEnumerable<PartyVotesCsvEntry> BuildPartyVotes(EndResultVotesEntry endResultEntry)
    {
        return endResultEntry.ListVotesEntries
            .SelectMany(x => BuildPartyVotes(x, endResultEntry))
            .GroupBy(x => x.PartyShortDescription)
            .Select(x =>
            {
                var first = x.First();
                return new PartyVotesCsvEntry(
                    first.PoliticalBusinessId,
                    first.NumberOfMandates,
                    first.DomainOfInfluenceName,
                    first.PartyShortDescription,
                    x.Sum(y => y.PartyVotes),
                    first.AccountedBallots,
                    first.BlankRowsOnListsWithoutParty);
            });
    }

    private IEnumerable<PartyVotesCsvEntry> BuildPartyVotes(ListVotesEntry listEntry, EndResultVotesEntry endResultEntry)
    {
        var candidateVoteCountPerParty = listEntry.CandidateEntries
            .GroupBy(c => c.PartyShortDescription)
            .ToDictionary(g => g.Key, g => g.Sum(y => y.VoteCount));

        // List contains only one party, we can take a shortcut since everything on this list belongs to the specified party.
        if (candidateVoteCountPerParty.Count == 1)
        {
            var partyEntry = candidateVoteCountPerParty.First();
            yield return new PartyVotesCsvEntry(
                endResultEntry.PoliticalBusinessId,
                endResultEntry.NumberOfMandates,
                endResultEntry.DomainOfInfluenceName,
                partyEntry.Key,
                partyEntry.Value + listEntry.ListBlankRowsCount,
                endResultEntry.AccountedBallots,
                endResultEntry.BlankRowsOnListsWithoutParty);
            yield break;
        }

        decimal totalCandidateCount = candidateVoteCountPerParty.Sum(y => y.Value);
        foreach (var (party, candidateCount) in candidateVoteCountPerParty)
        {
            var portionOfCandidateCount = candidateCount / totalCandidateCount;
            var blankRows = (int)Math.Round(portionOfCandidateCount * listEntry.ListBlankRowsCount);
            yield return new PartyVotesCsvEntry(
                endResultEntry.PoliticalBusinessId,
                endResultEntry.NumberOfMandates,
                endResultEntry.DomainOfInfluenceName,
                party,
                candidateCount + blankRows,
                endResultEntry.AccountedBallots,
                endResultEntry.BlankRowsOnListsWithoutParty);
        }
    }

    private class PartyVotesCsvEntry
    {
        public PartyVotesCsvEntry(Guid? politicalBusinessId, int numberOfMandates, string domainOfInfluenceName, string partyShortDescription, int partyVotes, int accountedBallots, int blankRowsOnListsWithoutParty)
        {
            PoliticalBusinessId = politicalBusinessId;
            NumberOfMandates = numberOfMandates;
            DomainOfInfluenceName = domainOfInfluenceName;
            PartyShortDescription = partyShortDescription;
            PartyVotes = partyVotes;
            AccountedBallots = accountedBallots;
            BlankRowsOnListsWithoutParty = blankRowsOnListsWithoutParty;
        }

        [Ignore]
        public Guid? PoliticalBusinessId { get; set; }

        [Ignore]
        public int NumberOfMandates { get; set; }

        [Name("Wahlkreis")]
        public string DomainOfInfluenceName { get; }

        [Name("Partei")]
        public string PartyShortDescription { get; }

        [Name("Parteistärke")]
        [Format("F2")]
        public decimal PartyStrength { get; set; }

        [Name("Fiktive Wählende (Partei)")]
        [Format("F2")]
        public decimal FictivePartyVoters { get; set; }

        [Name("Gültige Wahlzettel")]
        public int AccountedBallots { get; }

        [Name("Total abgegebene Stimmen")]
        public int PoliticalBusinessSubmittedVotes { get; set; }

        [Name("Parteistimmen")]
        public int PartyVotes { get; }

        [Name("Leere Stimmen")]
        public int BlankRowsOnListsWithoutParty { get; set; }
    }

    private class EndResultVotesEntry
    {
        public required Guid PoliticalBusinessId { get; init; }

        public required int NumberOfMandates { get; init; }

        public required string DomainOfInfluenceName { get; init; }

        public required int AccountedBallots { get; init; }

        public required int BlankRowsOnListsWithoutParty { get; init; }

        public required List<ListVotesEntry> ListVotesEntries { get; init; }
    }

    private class ListVotesEntry
    {
        public ListVotesEntry(int listBlankRowsCount, List<CandidateVotesEntry> candidateEntries)
        {
            ListBlankRowsCount = listBlankRowsCount;
            CandidateEntries = candidateEntries;
        }

        public int ListBlankRowsCount { get; }

        public List<CandidateVotesEntry> CandidateEntries { get; }
    }

    private class CandidateVotesEntry
    {
        public CandidateVotesEntry(int voteCount, string partyShortDescription)
        {
            VoteCount = voteCount;
            PartyShortDescription = partyShortDescription;
        }

        public int VoteCount { get; }

        public string PartyShortDescription { get; }
    }
}
