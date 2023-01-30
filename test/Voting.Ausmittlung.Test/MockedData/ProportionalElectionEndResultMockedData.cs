// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.Utils;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ProportionalElectionEndResultMockedData
{
    public const string ElectionId = "ee6ee245-4cde-4474-bc2b-c3e6ee164c62";

    public const string ListId1 = "d961fdd7-7910-4722-ac3e-8ae3d906741d";
    public const string ListId2 = "e960cb37-ea85-4464-9e91-26f5d77ebbfb";
    public const string ListId3 = "29ac4e1b-e629-4841-942b-5500ec3d7f57";
    public const string ListId4 = "68cf7a77-d2ae-4a2a-9c1e-301fb696e0a3";

    public const string List1CandidateId1 = "72680f65-d150-4ff4-90bd-66bb289c400a";
    public const string List1CandidateId2 = "7792ecfc-51ee-47e2-bc83-1eef92b6925a";
    public const string List1CandidateId3 = "170c718a-4433-4aa7-9583-2b79a5c3a550";
    public const string List2CandidateId1 = "e64e110d-2b36-4623-a5c0-b5a55c588c7a";
    public const string List2CandidateId2 = "f2dc5df8-fe5f-47fb-9d32-aae3acf64e6f";
    public const string List2CandidateId3 = "ef343dc0-b6bd-4dd2-bdd0-cc86f1dd30b9";
    public const string List3CandidateId1 = "759a921c-f434-4bf8-b444-2beb01c8166e";
    public const string List3CandidateId2 = "4d34f9c2-0627-4659-aca5-e4596e80bd16";
    public const string List3CandidateId3 = "88dc769e-f5a5-4909-955d-b432d64c6f99";
    public const string List4CandidateId1 = "bbaafd49-a554-4944-b8b9-d664bf1364db";
    public const string List4CandidateId2 = "cba76cb4-7b66-49ee-988d-e2e05d426b5a";

    public const string ListUnionId1 = "c46de46e-e319-43fb-8433-d571bc459dce";
    public const string SubListUnionId1 = "504931b4-4ca3-491e-ad09-61719bf48590";

    public static readonly Guid ElectionGuid = Guid.Parse(ElectionId);

    public static readonly Guid StGallenResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidStGallen, false);
    public static readonly Guid StGallenStFidenResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidStGallenStFiden, false);
    public static readonly Guid StGallenHaggenResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidStGallenHaggen, false);
    public static readonly Guid StGallenAuslandschweizerResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidStGallenAuslandschweizer, false);
    public static readonly Guid GossauResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidGossau, false);
    public static readonly Guid UzwilResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidUzwil, false);

    public static readonly string StGallenResultId = StGallenResultGuid.ToString();
    public static readonly string StGallenStFidenResultId = StGallenStFidenResultGuid.ToString();
    public static readonly string StGallenHaggenResultId = StGallenHaggenResultGuid.ToString();
    public static readonly string StGallenAuslandschweizerResultId = StGallenAuslandschweizerResultGuid.ToString();
    public static readonly string GossauResultId = GossauResultGuid.ToString();
    public static readonly string UzwilResultId = UzwilResultGuid.ToString();

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, ProportionalElectionMandateAlgorithm mandateAlgorithm, int numberOfMandates)
    {
        var election = BuildElection(
            mandateAlgorithm,
            numberOfMandates);
        election.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(election.ContestId, election.DomainOfInfluenceId);

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.ProportionalElections.Add(election);
            await db.SaveChangesAsync();
        });

        await runScoped(sp => sp.GetRequiredService<SimplePoliticalBusinessBuilder<ProportionalElection>>().Create(election));

        await runScoped(sp =>
            sp.GetRequiredService<ProportionalElectionResultBuilder>().RebuildForElection(election.Id, Guid.Parse(DomainOfInfluenceMockedData.IdStGallen), false));

        await runScoped(sp =>
            sp.GetRequiredService<ProportionalElectionEndResultInitializer>().RebuildForElection(election.Id, false));
    }

    public static ProportionalElection BuildElection(
        ProportionalElectionMandateAlgorithm mandateAlgorithm,
        int numberOfMandates)
    {
        return new ProportionalElection
        {
            Id = Guid.Parse(ElectionId),
            PoliticalBusinessNumber = "201",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl",
                (t, s) => t.ShortDescription = s,
                "Proporzwahl"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = false,
            MandateAlgorithm = mandateAlgorithm,
            NumberOfMandates = numberOfMandates,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
                {
                    BuildList(
                        ListId1,
                        1,
                        numberOfMandates,
                        new List<ProportionalElectionCandidate>
                        {
                            BuildCandidate(List1CandidateId1, 1, 1),
                            BuildCandidate(List1CandidateId2, 1, 2),
                            BuildCandidate(List1CandidateId3, 1, 3),
                        }),
                    BuildList(
                        ListId2,
                        2,
                        numberOfMandates,
                        new List<ProportionalElectionCandidate>
                        {
                            BuildCandidate(List2CandidateId1, 2, 1),
                            BuildCandidate(List2CandidateId2, 2, 2),
                            BuildCandidate(List2CandidateId3, 2, 3),
                        }),
                    BuildList(
                        ListId3,
                        3,
                        numberOfMandates,
                        new List<ProportionalElectionCandidate>
                        {
                            BuildCandidate(List3CandidateId1, 3, 1),
                            BuildCandidate(List3CandidateId2, 3, 2),
                            BuildCandidate(List3CandidateId3, 3, 3),
                        }),
                    BuildList(
                        ListId4,
                        4,
                        numberOfMandates,
                        new List<ProportionalElectionCandidate>
                        {
                            BuildCandidate(List4CandidateId1, 4, 1),
                            BuildCandidate(List4CandidateId2, 4, 2),
                        }),
                },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
                {
                    BuildListUnion(
                        ListUnionId1,
                        1,
                        new List<string>
                        {
                            ListId2,
                            ListId3,
                            ListId4,
                        }),
                    BuildSubListUnion(
                        SubListUnionId1,
                        ListUnionId1,
                        1,
                        new List<string>
                        {
                            ListId3,
                            ListId4,
                        }),
                },
        };
    }

    private static ProportionalElectionListUnion BuildListUnion(
        string listUnionId,
        int position,
        List<string> listIds)
    {
        var listUnionEntries = listIds.Select(id =>
            new ProportionalElectionListUnionEntry
            {
                Id = Guid.NewGuid(),
                ProportionalElectionListId = Guid.Parse(id),
            }).ToList();

        return new ProportionalElectionListUnion
        {
            Id = Guid.Parse(listUnionId),
            Position = position,
            ProportionalElectionListUnionEntries = listUnionEntries,
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                        (t, o) => t.Description = o,
                        $"Listenverbindung {position}"),
        };
    }

    private static ProportionalElectionListUnion BuildSubListUnion(
        string listUnionId,
        string rootListUnionId,
        int position,
        List<string> listIds)
    {
        var listUnionEntries = listIds.Select(id =>
            new ProportionalElectionListUnionEntry
            {
                Id = Guid.NewGuid(),
                ProportionalElectionListId = Guid.Parse(id),
            }).ToList();

        return new ProportionalElectionListUnion
        {
            Id = Guid.Parse(listUnionId),
            ProportionalElectionRootListUnionId = Guid.Parse(rootListUnionId),
            Position = position,
            ProportionalElectionListUnionEntries = listUnionEntries,
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                        (t, o) => t.Description = o,
                        $"Unterlistenverbindung {position}"),
        };
    }

    private static ProportionalElectionList BuildList(
        string listId,
        int position,
        int numberOfSeats,
        List<ProportionalElectionCandidate> candidates)
    {
        return new ProportionalElectionList
        {
            Id = Guid.Parse(listId),
            Position = position,
            BlankRowCount = numberOfSeats - candidates.Count,
            OrderNumber = $"L{position}",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                        (t, s) => t.ShortDescription = s,
                        $"Liste {position}"),
            ProportionalElectionCandidates = candidates,
        };
    }

    private static ProportionalElectionCandidate BuildCandidate(
        string candidateId,
        int listPosition,
        int candidatePosition)
    {
        var position = $"L{listPosition}.C{candidatePosition}";
        return new ProportionalElectionCandidate
        {
            Id = Guid.Parse(candidateId),
            FirstName = $"{position}firstName",
            LastName = $"{position}lastName",
            PoliticalFirstName = $"{position}pol firstName",
            PoliticalLastName = $"{position}pol last name",
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
            Incumbent = true,
            Position = candidatePosition,
            Locality = "locality",
            Number = $"number{position}",
            Sex = SexType.Female,
            Title = "title",
            ZipCode = "zip code",
            Origin = "origin",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                (t, o) => t.Occupation = o,
                "occupation",
                (t, o) => t.OccupationTitle = o,
                "occupation title"),
        };
    }
}
