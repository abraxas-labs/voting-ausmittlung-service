// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.Utils;

namespace Voting.Ausmittlung.Test.MockedData;

/// <summary>
/// Mock Data according the attachment "Proporzwahl - Ebene Bund - 5a_Verteilung der Sitze.pdf" in jira ticket VOTING-302.
/// </summary>
public static class ProportionalElectionEndResultSgExampleMockedData
{
    public const string IdStGallenNationalratElection = "175251eb-8a24-4a16-939f-2368856cd8b7";

    private const int NumberOfMandates = 12;
    private const int CountOfVotersMale = 153395;
    private const int CountOfVotersFemale = 164574;
    private const int TotalCountOfVoters = CountOfVotersMale + CountOfVotersFemale;

    private static readonly int[][] ListUnionNumbers =
    {
            new[] { 1, 3, 4, 6, 14 },
            new[] { 2, 7, 13 },
            new[] { 8, 19 },
            new[] { 9, 10, 11, 12 },
            new[] { 16, 17, 21 },
    };

    private static readonly int[][] ListSubUnionNumbers =
    {
            new[] { 1, 3 },
            new[] { 6, 14 },
            new[] { 9, 10 },
            new[] { 11, 12 },
            new[] { 16, 17 },
    };

    private static readonly (string, int)[] ListNameAndVoteCounts =
    {
            ("JCVP", 26_843),
            ("JFSG", 18_024),
            ("CVP", 259_922),
            ("EVP", 30_333),
            ("PFSG", 8_519),
            ("BDP SG", 57_133),
            ("FDP", 214_205),
            ("SVP", 620_183),
            ("SP", 231_302),
            ("JUSO", 14_356),
            ("GRÜNE", 76_606),
            ("JUGRÜ", 22_941),
            ("UFS", 15_400),
            ("JBDP SG", 5_836),
            ("IP", 8_533),
            ("glp", 70_633),
            ("jglp", 14_040),
            ("SBösch", 4_547),
            ("EDU", 16_134),
            ("DPS", 2_329),
            ("PP", 6_924),
            ("SD", 4_808),
            ("MGiger", 2_905),
    };

    public static ProportionalElection StGallenNationalratElection => CreateElection();

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        var election = StGallenNationalratElection;
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var simplePbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<ProportionalElection>>();

            var mappedDomainOfInfluence = await db.DomainOfInfluences.FirstAsync(doi =>
                doi.SnapshotContestId == election.ContestId &&
                doi.BasisDomainOfInfluenceId == election.DomainOfInfluenceId);
            election.DomainOfInfluenceId = mappedDomainOfInfluence.Id;

            db.ProportionalElections.Add(election);
            await db.SaveChangesAsync();

            await simplePbBuilder.Create(election);

            var resultBuilder = sp.GetRequiredService<ProportionalElectionResultBuilder>();
            await resultBuilder.RebuildForElection(election.Id, mappedDomainOfInfluence.Id, false);

            var endResultInitializer = sp.GetRequiredService<ProportionalElectionEndResultInitializer>();
            var endResultBuilder = sp.GetRequiredService<ProportionalElectionEndResultBuilder>();
            await endResultInitializer.RebuildForElection(election.Id, false);

            var contestDetail = await db.ContestDetails
                .AsTracking()
                .AsSplitQuery()
                .Where(x => x.ContestId == election.ContestId)
                .Include(x => x.VotingCards)
                .Include(x => x.CountOfVotersInformationSubTotals)
                .FirstAsync();

            contestDetail.TotalCountOfVoters = TotalCountOfVoters;
            contestDetail.CountOfVotersInformationSubTotals.Clear();
            contestDetail.CountOfVotersInformationSubTotals.Add(new ContestCountOfVotersInformationSubTotal
            {
                Sex = SexType.Female,
                VoterType = VoterType.Swiss,
                CountOfVoters = CountOfVotersFemale,
            });
            contestDetail.CountOfVotersInformationSubTotals.Add(new ContestCountOfVotersInformationSubTotal
            {
                Sex = SexType.Male,
                VoterType = VoterType.Swiss,
                CountOfVoters = CountOfVotersMale,
            });
            await db.SaveChangesAsync();

            var result = await db.ProportionalElectionResults
                .AsTracking()
                .AsSplitQuery()
                .Where(x => x.ProportionalElectionId == election.Id)
                .Include(x => x.ListResults)
                .ThenInclude(x => x.CandidateResults)
                .FirstAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidStGallen);

            result.State = CountingCircleResultState.SubmissionDone;
            result.TotalCountOfVoters = TotalCountOfVoters;
            result.CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalReceivedBallots = 110_812,
                ConventionalInvalidBallots = 1498,
                ConventionalBlankBallots = 80,
                ConventionalAccountedBallots = 109_224,
                EVotingReceivedBallots = 36937,
                EVotingInvalidBallots = 500,
                EVotingBlankBallots = 40,
                EVotingAccountedBallots = 36407,
                VoterParticipation = .4647m,
            };

            var listPosition = 1;
            var emptyVoteCount = 15_116;
            foreach (var (_, voteCount) in ListNameAndVoteCounts)
            {
                var listEmptyVoteCount = Math.Min((int)(voteCount * .1), emptyVoteCount);
                emptyVoteCount -= listEmptyVoteCount;
                SetVoteCounts(result, listPosition++, voteCount, listEmptyVoteCount);
            }

            if (emptyVoteCount != 0)
            {
                throw new InvalidOperationException("not all empty votes are distributed...");
            }

            await db.SaveChangesAsync();

            await endResultBuilder.AdjustEndResult(result.Id, false);
            await db.SaveChangesAsync();
        });
    }

    private static ProportionalElection CreateElection()
    {
        var election = new ProportionalElection
        {
            Id = Guid.Parse(IdStGallenNationalratElection),
            PoliticalBusinessNumber = "100",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Nationalratswahl",
                (t, s) => t.ShortDescription = s,
                "Nationalratswahl"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = NumberOfMandates,
            ProportionalElectionLists = GenerateLists(),
        };

        election.ProportionalElectionListUnions = GenerateListUnions(election.ProportionalElectionLists);

        return election;
    }

    private static List<ProportionalElectionList> GenerateLists()
    {
        return ListNameAndVoteCounts
            .Select((list, i) => new ProportionalElectionList
            {
                Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                        (t, o) => t.Description = o,
                        $"Liste {(i + 1).ToString("00", CultureInfo.InvariantCulture)} ({list.Item1})",
                        (t, s) => t.ShortDescription = s,
                        list.Item1),
                BlankRowCount = GetNrOfEmptyRows(i + 1),
                ProportionalElectionCandidates = GenerateCandidates(i + 1),
                Position = i + 1,
                OrderNumber = (i + 1).ToString("00", CultureInfo.InvariantCulture),
            })
            .ToList();
    }

    private static List<ProportionalElectionListUnion> GenerateListUnions(IEnumerable<ProportionalElectionList> lists)
    {
        var listUnions = new List<ProportionalElectionListUnion>();
        var listsByNr = lists.ToDictionary(l => l.Position);

        var listUnionCounter = 1;
        foreach (var listUnionGroup in ListUnionNumbers)
        {
            var mainList = listsByNr[listUnionGroup[0]];
            var listUnionEntries = listUnionGroup
                .Select(x => new ProportionalElectionListUnionEntry
                {
                    ProportionalElectionList = listsByNr[x],
                })
                .ToList();
            var listUnion = new ProportionalElectionListUnion
            {
                Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                        (t, o) => t.Description = o,
                        $"ListUnion {listUnionCounter++}"),
                Position = listUnionCounter,
                ProportionalElectionMainList = mainList,
                ProportionalElectionListUnionEntries = listUnionEntries,
            };

            var mainEntry = listUnionEntries[0];
            mainEntry.ProportionalElectionListUnion = listUnion;
            mainList.ProportionalElectionListUnionEntries.Add(mainEntry);
            listUnions.Add(listUnion);
        }

        var listSubUnionCounter = 1;
        foreach (var listSubUnionGroup in ListSubUnionNumbers)
        {
            listUnions.Add(new ProportionalElectionListUnion
            {
                Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                        (t, o) => t.Description = o,
                        $"ListSubUnion {listSubUnionCounter++}"),
                Position = listSubUnionCounter + listUnionCounter,
                ProportionalElectionRootListUnion = listsByNr[listSubUnionGroup[0]].ProportionalElectionListUnion,
                ProportionalElectionMainList = listsByNr[listSubUnionGroup[0]],
                ProportionalElectionListUnionEntries = listSubUnionGroup
                                   .Select(x => new ProportionalElectionListUnionEntry
                                   {
                                       ProportionalElectionList = listsByNr[x],
                                   })
                                   .ToList(),
            });
        }

        return listUnions;
    }

    private static int GetNrOfEmptyRows(int listNr)
        => Math.Max((listNr % NumberOfMandates) - 5, 0); // at least 5 candidates per list

    private static List<ProportionalElectionCandidate> GenerateCandidates(int listNr)
    {
        return Enumerable.Range(1, NumberOfMandates - GetNrOfEmptyRows(listNr))
            .Select(i => new ProportionalElectionCandidate
            {
                FirstName = $"FN {listNr}.{i}",
                LastName = $"LN {listNr}.{i}",
            })
            .ToList();
    }

    private static void SetVoteCounts(
        ProportionalElectionResult result,
        int listPosition,
        int voteCount,
        int blankRowsCount)
    {
        var listResult = result.ListResults.Single(x => x.List.Position == listPosition);
        var candidateResult = listResult.CandidateResults.First();

        var eVotingVoteCount = voteCount / 4;
        var eVotingBlankRowsCount = blankRowsCount / 4;

        SetSubTotalVoteCounts(
            result.ConventionalSubTotal,
            listResult.ConventionalSubTotal,
            candidateResult.ConventionalSubTotal,
            voteCount - eVotingVoteCount,
            blankRowsCount - eVotingBlankRowsCount);

        SetSubTotalVoteCounts(
            result.EVotingSubTotal,
            listResult.EVotingSubTotal,
            candidateResult.EVotingSubTotal,
            eVotingVoteCount,
            eVotingBlankRowsCount);
    }

    private static void SetSubTotalVoteCounts(
        ProportionalElectionResultSubTotal electionResultSubTotal,
        ProportionalElectionListResultSubTotal listResultSubTotal,
        ProportionalElectionCandidateResultSubTotal candidateResultSubTotal,
        int voteCount,
        int blankRowsCount)
    {
        // these numbers may not be exactly correct but they are an approximation which should be enough for these mocks
        voteCount -= blankRowsCount;
        candidateResultSubTotal.ModifiedListVotesCount = (int)(voteCount * .8);
        candidateResultSubTotal.UnmodifiedListVotesCount = voteCount - candidateResultSubTotal.ModifiedListVotesCount;

        listResultSubTotal.ModifiedListVotesCount = candidateResultSubTotal.ModifiedListVotesCount;
        listResultSubTotal.ListVotesCountOnOtherLists = (int)(candidateResultSubTotal.ModifiedListVotesCount * .2);
        listResultSubTotal.UnmodifiedListVotesCount = candidateResultSubTotal.UnmodifiedListVotesCount;
        listResultSubTotal.ModifiedListBlankRowsCount = blankRowsCount / 2;
        listResultSubTotal.UnmodifiedListBlankRowsCount = blankRowsCount - listResultSubTotal.ModifiedListBlankRowsCount;
        listResultSubTotal.UnmodifiedListsCount = listResultSubTotal.UnmodifiedListVotesCount / NumberOfMandates;
        listResultSubTotal.ModifiedListsCount = listResultSubTotal.ModifiedListVotesCount / NumberOfMandates;

        electionResultSubTotal.TotalCountOfUnmodifiedLists += listResultSubTotal.UnmodifiedListVotesCount / NumberOfMandates;
        electionResultSubTotal.TotalCountOfModifiedLists += listResultSubTotal.ModifiedListVotesCount / NumberOfMandates;
        electionResultSubTotal.TotalCountOfListsWithoutParty += blankRowsCount / NumberOfMandates;
        electionResultSubTotal.TotalCountOfBlankRowsOnListsWithoutParty += listResultSubTotal.BlankRowsCount;
    }
}
