// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;

namespace Voting.Ausmittlung.Test.MockedData;

/// <summary>
/// Mocked Data for ProportionalElectionUnions:
/// - Gossau with Lists (01a, 01b, 02)
/// - Uzwil with Lists (01a, 01b).
/// </summary>
public static class ProportionalElectionUnionEndResultMockedData
{
    public const string UnionId = "7eb98947b5f3406d95a9dd99fc5609f3";

    public const string GossauElectionId = "b192af77c0ea408a94c861ff99808172";
    public const string UzwilElectionId = "be9aee15c36a4da6a9a85b5bd206b6e7";

    public const string GossauListId1a = "dd77d6d38dce4f0d85dc88d977c142ce";
    public const string GossauListId1b = "83a5670e1ee64906840894e9c837d306";
    public const string GossauListId2 = "5ea385f42ad84b8886d98df60859715d";
    public const string GossauListId3 = "5bb385f42ad84b8886d98df60859715d";
    public const string UzwilListId1a = "d860db7f6747406a92e977914d671131";
    public const string UzwilListId1b = "43b8961375984633a3e17cb82d834c33";
    public const string UzwilListId2 = "1e58741c6dc846288d012d0a92a70c11";
    public const string UzwilListId3 = "99ee20ab7e664765a2d7f60234197999";

    public const string GossauList1aCandidateId1 = "48f01277fb3d443d9b778623233e3fda";
    public const string GossauList1aCandidateId2 = "2da005293dca4dda8467b41a8e78a10e";
    public const string GossauList1aCandidateId3 = "8fbdc790a178412784e246da15173b7e";
    public const string GossauList1bCandidateId1 = "67d86e3e3e6d4a27abbbe94d77a3447e";
    public const string GossauList1bCandidateId2 = "65a6494164d248949ee6eee039357f4b";
    public const string GossauList1bCandidateId3 = "0d401d6b138f4eb3891ba869cf96d1fb";
    public const string GossauList2CandidateId1 = "f4765b6f762d4b47a4e82b1ee9a65e85";
    public const string GossauList2CandidateId2 = "8a2381cb9fcc4c918df3855f2a73dfb5";
    public const string UzwilList1aCandidateId1 = "85f72dc4b2ec494f972f5c71d64bfc0c";
    public const string UzwilList1aCandidateId2 = "3179cfc0fa974e7f9dfd4a35efcdf906";
    public const string UzwilList1bCandidateId1 = "013811909bb54921b618fda24ecf5d6c";
    public const string UzwilList1bCandidateId2 = "24cd4ad3bc34415d8d8b2059fd4a94c7";
    public const string UzwilList2CandidateId1 = "45d30a1d812e4838ac9b76529d12df72";
    public const string UzwilList2CandidateId2 = "29fa23975c324f71abb290d00fe785b2";
    public const string UzwilList3CandidateId1 = "489f8a2ef47d4928b9a605636f8be0c2";

    public static ProportionalElection Gossau =>
        new ProportionalElection
        {
            Id = Guid.Parse(GossauElectionId),
            PoliticalBusinessNumber = "01",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Kantonratswahl",
                (t, s) => t.ShortDescription = s,
                "Kantonratswahl"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
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
            NumberOfMandates = 3,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(GossauListId1a),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 1a SP/JUSO Frauen",
                            (t, s) => t.ShortDescription = s,
                            "Liste 1a"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(GossauList1aCandidateId1),
                                FirstName = "Bettina",
                                LastName = "Surber",
                                PoliticalFirstName = "Bettina",
                                PoliticalLastName = "Surber",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "01",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(GossauList1aCandidateId2),
                                FirstName = "Maria",
                                LastName = "Pappa",
                                PoliticalFirstName = "Maria",
                                PoliticalLastName = "Pappa",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 2,
                                Locality = "locality",
                                Number = "02",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(GossauList1aCandidateId3),
                                FirstName = "Itta",
                                LastName = "Loher",
                                PoliticalFirstName = "Itta",
                                PoliticalLastName = "Loher",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 3,
                                Locality = "locality",
                                Number = "03",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(GossauListId1b),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1b",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 1b SP/JUSO Männer",
                            (t, s) => t.ShortDescription = s,
                            "Liste 1b"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(GossauList1bCandidateId1),
                                FirstName = "Ruedi",
                                LastName = "Blumer",
                                PoliticalFirstName = "Ruedi",
                                PoliticalLastName = "Blumer",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "01",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(GossauList1bCandidateId2),
                                FirstName = "Jans",
                                LastName = "Peter",
                                PoliticalFirstName = "Jans",
                                PoliticalLastName = "Peter",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 2,
                                Locality = "locality",
                                Number = "02",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(GossauList1bCandidateId3),
                                FirstName = "Daniel",
                                LastName = "Kehl",
                                PoliticalFirstName = "Daniel",
                                PoliticalLastName = "Kehl",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 3,
                                Locality = "locality",
                                Number = "03",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(GossauListId2),
                        Position = 2,
                        BlankRowCount = 0,
                        OrderNumber = "2",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 2 SVP",
                            (t, s) => t.ShortDescription = s,
                            "Liste 2"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(GossauList2CandidateId1),
                                FirstName = "Karl",
                                LastName = "Güntzel",
                                PoliticalFirstName = "Karl",
                                PoliticalLastName = "Güntzel",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "01",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSvp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(GossauList2CandidateId2),
                                FirstName = "Christian",
                                LastName = "Koller",
                                PoliticalFirstName = "Christian",
                                PoliticalLastName = "Koller",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 2,
                                Locality = "locality",
                                Number = "02",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSvp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(GossauListId3),
                        Position = 3,
                        BlankRowCount = 1,
                        OrderNumber = "3",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 3 SP/JUSO Männer Studenten",
                            (t, s) => t.ShortDescription = s,
                            "Liste 3"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse("55c87ffc-737b-4e94-9ed2-bad902a86ede"),
                                FirstName = "Franz-Josef",
                                LastName = "Muster",
                                PoliticalFirstName = "Franz-Josef",
                                PoliticalLastName = "Muster",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "01",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "SP"),
                        Id = Guid.Parse("1d2e23ed-d26b-47ac-8b72-a89eebfb7c8f"),
                        Position = 1,
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry { ProportionalElectionListId = Guid.Parse(GossauListId1a) },
                            new ProportionalElectionListUnionEntry { ProportionalElectionListId = Guid.Parse(GossauListId1b) },
                        },
                    },
                    new ProportionalElectionListUnion
                    {
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "SP2"),
                        Id = Guid.Parse("8baa25a1-0e52-4a1a-a1a4-d19c59ad0e45"),
                        Position = 2,
                        ProportionalElectionRootListUnionId = Guid.Parse("1d2e23ed-d26b-47ac-8b72-a89eebfb7c8f"),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry { ProportionalElectionListId = Guid.Parse(GossauListId1a) },
                            new ProportionalElectionListUnionEntry { ProportionalElectionListId = Guid.Parse(GossauListId3) },
                        },
                    },
            },
        };

    public static ProportionalElection Uzwil =>
        new ProportionalElection
        {
            Id = Guid.Parse(UzwilElectionId),
            PoliticalBusinessNumber = "01",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Kantonratswahl",
                (t, s) => t.ShortDescription = s,
                "Kantonratswahl"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
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
            NumberOfMandates = 2,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(UzwilListId1a),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 1a SP/JUSO Frauen",
                            (t, s) => t.ShortDescription = s,
                            "Liste 1a"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(UzwilList1aCandidateId1),
                                FirstName = "Lara",
                                LastName = "Weibel",
                                PoliticalFirstName = "Lara",
                                PoliticalLastName = "Weibel",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "01",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(UzwilList1aCandidateId2),
                                FirstName = "Doris",
                                LastName = "Königer",
                                PoliticalFirstName = "Doris",
                                PoliticalLastName = "Königer",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 2,
                                Locality = "locality",
                                Number = "02",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(UzwilListId1b),
                        Position = 2,
                        BlankRowCount = 0,
                        OrderNumber = "1b",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 1b SP/JUSO Männer",
                            (t, s) => t.ShortDescription = s,
                            "Liste 1b"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(UzwilList1bCandidateId1),
                                FirstName = "Florian",
                                LastName = "Kobler",
                                PoliticalFirstName = "Florian",
                                PoliticalLastName = "Kobler",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "01",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(UzwilList1bCandidateId2),
                                FirstName = "Tobias",
                                LastName = "Kindler",
                                PoliticalFirstName = "Tobias",
                                PoliticalLastName = "Kindler",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 2,
                                Locality = "locality",
                                Number = "02",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(UzwilListId2),
                        Position = 3,
                        BlankRowCount = 0,
                        OrderNumber = "2",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 2, GLP",
                            (t, s) => t.ShortDescription = s,
                            "Liste 2"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(UzwilList2CandidateId1),
                                FirstName = "Martin",
                                LastName = "Herz",
                                PoliticalFirstName = "Martin",
                                PoliticalLastName = "Herz",
                                DateOfBirth = new DateTime(1954, 7, 14, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "Uzwil",
                                Number = "01",
                                Sex = SexType.Male,
                                Title = "Dr.",
                                ZipCode = "9100",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "Netzwerkadministrator"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(UzwilList2CandidateId2),
                                FirstName = "Luca",
                                LastName = "Ritter",
                                PoliticalFirstName = "Luca",
                                PoliticalLastName = "Ritter",
                                DateOfBirth = new DateTime(1958, 7, 14, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 2,
                                Locality = "Uzwil",
                                Number = "02",
                                Sex = SexType.Male,
                                ZipCode = "9100",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "Strassenbauer"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(UzwilListId3),
                        Position = 4,
                        BlankRowCount = 1,
                        OrderNumber = "3",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 3, die Internetpartei",
                            (t, s) => t.ShortDescription = s,
                            "Liste 3"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(UzwilList3CandidateId1),
                                FirstName = "Stephanie",
                                LastName = "Gaertner",
                                PoliticalFirstName = "Stephanie",
                                PoliticalLastName = "Gaertner",
                                DateOfBirth = new DateTime(1982, 7, 14, 0, 0, 0, DateTimeKind.Utc),
                                Position = 1,
                                Locality = "Uzwil",
                                Number = "01",
                                Sex = SexType.Female,
                                ZipCode = "9100",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "Coiffeuse"),
                                PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundSvp),
                                Origin = "origin",
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
            },
        };

    public static IEnumerable<ProportionalElection> AllElections
    {
        get
        {
            yield return Gossau;
            yield return Uzwil;
        }
    }

    public static ProportionalElectionUnion Union =>
        new ProportionalElectionUnion
        {
            Id = Guid.Parse(UnionId),
            Description = "Kantonratswahl",
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            SecureConnectId = SecureConnectTestDefaults.MockedTenantBund.Id,
            ProportionalElectionUnionEntries =
            {
                    new ProportionalElectionUnionEntry
                    {
                        Id = Guid.Parse("af67bed3d196452c9164d80a85c1e7e1"),
                        ProportionalElectionId = Gossau.Id,
                    },
                    new ProportionalElectionUnionEntry
                    {
                        Id = Guid.Parse("227b5b5c23474710938f6d200d06e182"),
                        ProportionalElectionId = Uzwil.Id,
                    },
            },
        };

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var simplePbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<ProportionalElection>>();
            foreach (var election in AllElections)
            {
                var testingPhaseEnded = ContestMockedData.TestingPhaseEnded(election.ContestId);
                var mappedDomainOfInfluence = await db.DomainOfInfluences.FirstAsync(doi =>
                    doi.SnapshotContestId == election.ContestId && doi.BasisDomainOfInfluenceId == election.DomainOfInfluenceId);
                election.DomainOfInfluenceId = mappedDomainOfInfluence.Id;

                db.ProportionalElections.Add(election);
                await db.SaveChangesAsync();

                await simplePbBuilder.Create(election);

                await sp.GetRequiredService<ProportionalElectionResultBuilder>()
                    .RebuildForElection(election.Id, mappedDomainOfInfluence.Id, testingPhaseEnded, election.ContestId);
                var endResultInitializer = sp.GetRequiredService<ProportionalElectionEndResultInitializer>();
                var endResultBuilder = sp.GetRequiredService<ProportionalElectionEndResultBuilder>();
                await endResultInitializer.RebuildForElection(election.Id, testingPhaseEnded);

                var result = await db.ProportionalElectionResults
                    .AsTracking()
                    .AsSplitQuery()
                    .Where(x => x.ProportionalElectionId == election.Id)
                    .Include(x => x.ListResults)
                    .ThenInclude(x => x.CandidateResults)
                    .Include(x => x.ListResults)
                    .ThenInclude(x => x.List)
                    .FirstOrDefaultAsync();

                if (result!.ProportionalElectionId == Gossau.Id)
                {
                    SetGossauResultMockData(result);
                }
                else if (result.ProportionalElectionId == Uzwil.Id)
                {
                    SetUzwilResultMockData(result);
                }

                await db.SaveChangesAsync();

                await endResultBuilder.AdjustEndResult(result.Id, false, true);
                await endResultBuilder.DistributeNumberOfMandates(result.ProportionalElectionId);
                await db.SaveChangesAsync();
            }

            db.ProportionalElectionUnions.Add(Union);
            await db.SaveChangesAsync();

            await sp.GetRequiredService<ProportionalElectionUnionListBuilder>().RebuildLists(
                Union.Id,
                AllElections.Select(x => x.Id).ToList());
        });
    }

    private static void SetGossauResultMockData(ProportionalElectionResult result)
    {
        result.State = CountingCircleResultState.SubmissionDone;
        result.SubmissionDoneTimestamp = MockedClock.GetDate(hoursDelta: -1);
        result.TotalCountOfVoters = 2000;
        result.CountOfVoters = new PoliticalBusinessNullableCountOfVoters
        {
            ConventionalSubTotal = new PoliticalBusinessCountOfVotersNullableSubTotal
            {
                ReceivedBallots = 500,
                InvalidBallots = 200,
                BlankBallots = 80,
                AccountedBallots = 220,
            },
            VoterParticipation = .5m,
        };

        SetVoteCounts(result, GossauListId1a, GossauList1aCandidateId1, 5, 200, 100);
        SetVoteCounts(result, GossauListId1a, GossauList1aCandidateId2, 4, 80, 20);
        SetVoteCounts(result, GossauListId1a, GossauList1aCandidateId3, 3, 40, 10);
        SetVoteCounts(result, GossauListId1b, GossauList1bCandidateId1, 2, 298, 2);
        SetVoteCounts(result, GossauListId1b, GossauList1bCandidateId2, 1, 0, 0);
        SetVoteCounts(result, GossauListId1b, GossauList1bCandidateId3, 0, 0, 0);
        SetVoteCounts(result, GossauListId2, GossauList2CandidateId1, 10, 380, 20);
        SetVoteCounts(result, GossauListId2, GossauList2CandidateId2, 0, 1, 9);
    }

    private static void SetUzwilResultMockData(ProportionalElectionResult result)
    {
        result.State = CountingCircleResultState.AuditedTentatively;
        result.SubmissionDoneTimestamp = MockedClock.GetDate(hoursDelta: -6);
        result.AuditedTentativelyTimestamp = MockedClock.GetDate(hoursDelta: -3);
        result.TotalCountOfVoters = 1000;
        result.ConventionalSubTotal.TotalCountOfListsWithoutParty = 1;
        result.ConventionalSubTotal.TotalCountOfModifiedLists = 15;
        result.ConventionalSubTotal.TotalCountOfUnmodifiedLists = 7;
        result.ConventionalSubTotal.TotalCountOfBlankRowsOnListsWithoutParty = 2;
        result.EVotingSubTotal.TotalCountOfListsWithoutParty = 1;
        result.EVotingSubTotal.TotalCountOfModifiedLists = 5;
        result.EVotingSubTotal.TotalCountOfUnmodifiedLists = 3;
        result.CountOfVoters = new PoliticalBusinessNullableCountOfVoters
        {
            ConventionalSubTotal = new PoliticalBusinessCountOfVotersNullableSubTotal
            {
                ReceivedBallots = 190,
                InvalidBallots = 80,
                BlankBallots = 30,
                AccountedBallots = 80,
            },
            EVotingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
            {
                ReceivedBallots = 60,
                InvalidBallots = 20,
                BlankBallots = 10,
                AccountedBallots = 30,
            },
            VoterParticipation = .5m,
        };

        SetVoteCounts(
            result,
            UzwilListId1a,
            UzwilList1aCandidateId1,
            10,
            100,
            150,
            (Guid.Parse(UzwilListId1a), 7),
            (null, 8),
            (Guid.Parse(UzwilListId1b), 10),
            (Guid.Parse(UzwilListId2), 3),
            (Guid.Parse(UzwilListId3), 9));
        SetVoteCounts(result, UzwilListId1a, UzwilList1aCandidateId2, 5, 15, 5);
        SetVoteCounts(
            result,
            UzwilListId1b,
            UzwilList1bCandidateId1,
            0,
            298,
            2,
            (Guid.Parse(UzwilListId1b), 122),
            (null, 3),
            (Guid.Parse(UzwilListId1a), 5),
            (Guid.Parse(UzwilListId2), 12),
            (Guid.Parse(UzwilListId3), 7));
        SetVoteCounts(
            result,
            UzwilListId1b,
            UzwilList1bCandidateId2,
            0,
            24,
            1,
            (null, 2),
            (Guid.Parse(UzwilListId1a), 3),
            (Guid.Parse(UzwilListId2), 4),
            (Guid.Parse(UzwilListId3), 1));
        SetVoteCounts(
            result,
            UzwilListId2,
            UzwilList2CandidateId1,
            5,
            15,
            5,
            (null, 1),
            (Guid.Parse(UzwilListId1a), 2),
            (Guid.Parse(UzwilListId1b), 3));
        SetVoteCounts(
            result,
            UzwilListId2,
            UzwilList2CandidateId2,
            0,
            2,
            10,
            (Guid.Parse(UzwilListId1a), 1),
            (Guid.Parse(UzwilListId1b), 1));
    }

    private static void SetVoteCounts(
        ProportionalElectionResult result,
        string listIdStr,
        string candidateIdStr,
        int blankRowsCount,
        int modifiedVoteCount,
        int unmodifiedVoteCount,
        params (Guid?, int)[] voteSourceCounts)
    {
        var listId = Guid.Parse(listIdStr);
        var candidateId = Guid.Parse(candidateIdStr);

        var listResult = result.ListResults.Single(x => x.ListId == listId);
        var candidateResult = listResult.CandidateResults.Single(x => x.CandidateId == candidateId);

        var eVotingBlankRowsCount = blankRowsCount / 3;
        var eVotingModifiedVoteCount = modifiedVoteCount / 3;
        var eVotingUnmodifiedVoteCount = unmodifiedVoteCount / 3;

        SetSubTotalVoteCounts(
            listResult.ConventionalSubTotal,
            candidateResult.ConventionalSubTotal,
            blankRowsCount - eVotingBlankRowsCount,
            modifiedVoteCount - eVotingModifiedVoteCount,
            unmodifiedVoteCount - eVotingUnmodifiedVoteCount);

        SetSubTotalVoteCounts(
            listResult.EVotingSubTotal,
            candidateResult.EVotingSubTotal,
            eVotingBlankRowsCount,
            eVotingModifiedVoteCount,
            eVotingUnmodifiedVoteCount);

        if (listResult.List.Position % 2 == 0)
        {
            listResult.ConventionalSubTotal.ModifiedListsCount++;
        }
        else
        {
            listResult.ConventionalSubTotal.UnmodifiedListsCount++;
        }

        if (voteSourceCounts.Length > 0)
        {
            candidateResult.VoteSources = voteSourceCounts
                .Select(x => new ProportionalElectionCandidateVoteSourceResult
                {
                    ListId = x.Item1,
                    ConventionalVoteCount = x.Item2,
                }).ToList();
        }
    }

    private static void SetSubTotalVoteCounts(
        ProportionalElectionListResultSubTotal listResultSubTotal,
        ProportionalElectionCandidateResultSubTotal candidateResultSubTotal,
        int blankRowsCount,
        int modifiedVoteCount,
        int unmodifiedVoteCount)
    {
        candidateResultSubTotal.ModifiedListVotesCount = modifiedVoteCount;
        candidateResultSubTotal.UnmodifiedListVotesCount = unmodifiedVoteCount;
        candidateResultSubTotal.CountOfVotesOnOtherLists = (int)(modifiedVoteCount * .2);
        listResultSubTotal.ModifiedListBlankRowsCount += blankRowsCount;
        listResultSubTotal.ModifiedListVotesCount += modifiedVoteCount;
        listResultSubTotal.ListVotesCountOnOtherLists += (int)(modifiedVoteCount * .2);
        listResultSubTotal.UnmodifiedListVotesCount += unmodifiedVoteCount;
    }
}
