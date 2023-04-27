// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.Utils;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ProportionalElectionMockedData
{
    public const string IdBundProportionalElectionInContestBund = "053d1197-ddb2-4906-8c90-b9baa45a40fb";
    public const string IdStGallenProportionalElectionInContestBund = "3a832f45-34c0-47ce-b1e3-db27b97948ba";
    public const string IdUzwilProportionalElectionInContestBundWithoutChilds = "a73b1bf3-7bbe-44fb-9b65-8f5e1734ad72";
    public const string IdBundProportionalElectionInContestStGallen = "30e170ba-ed97-4886-93c9-ee35b106a22e";
    public const string IdGossauProportionalElectionInContestStGallen = "fa69e964-0a02-4d16-b417-247e8987021a";
    public const string IdUzwilProportionalElectionInContestStGallen = "da091f50-9f11-4deb-9621-948fbfbdc322";
    public const string IdStGallenProportionalElectionInContestStGallen = "8fd00ee5-cc68-4b33-86b0-cc9c58dc1b1f";
    public const string IdStGallenProportionalElectionInContestStGallenWithoutChilds = "06f7ecb7-e175-4c3b-9ea5-cfc138d08278";
    public const string IdGossauProportionalElectionInContestGossau = "81880186-febc-4fd7-bb82-62c446430027";
    public const string IdUzwilProportionalElectionInContestUzwilWithoutChilds = "76d1c7ed-85ec-4e62-a540-ca0a83149d32";
    public const string IdGenfProportionalElectionInContestBundWithoutChilds = "61eeda40-6669-4793-9831-ade34e516365";
    public const string IdKircheProportionalElectionInContestKirche = "62fc5770-ad6a-41bd-9375-008a1dc11939";
    public const string IdKircheProportionalElectionInContestKircheWithoutChilds = "27b52067-dcb9-4701-87cc-54d70bc653f4";

    public const string ListIdBundProportionalElectionInContestBund = "5af18d6d-83b7-40c6-997f-248359817a0d";
    public const string List1IdStGallenProportionalElectionInContestBund = "6fa5262f-bf27-4eb9-81d4-23bb1a49d031";
    public const string List2IdStGallenProportionalElectionInContestBund = "05b72caf-23a9-411d-bac3-7d587666b48a";
    public const string ListIdBundProportionalElectionInContestStGallen = "ead283f5-5b06-4d94-b23a-1ddf8fa9079f";
    public const string ListIdUzwilProportionalElectionInContestStGallen = "66dbbea3-0c99-469f-94c5-4314c32e8eab";
    public const string ListIdStGallenProportionalElectionInContestStGallen = "6eedf849-0ecc-4a02-a43b-99ef4b11d795";
    public const string ListId1GossauProportionalElectionInContestStGallen = "9091a3b6-3785-4adc-a486-f486e686503e";
    public const string ListId2GossauProportionalElectionInContestStGallen = "bfe54c2a-6bdf-41a3-bf11-321203c380d3";
    public const string ListId3GossauProportionalElectionInContestStGallen = "afebb285-599d-415f-89ac-04ebcbc4eaeb";
    public const string ListIdGossauProportionalElectionInContestGossau = "84a0c2dd-9c18-4a64-a08f-d2478c0d3a5b";
    public const string ListIdUzwilProportionalElectionInContestUzwil = "3808a9cc-c523-40d8-b341-230d801be63b";
    public const string ListId2UzwilProportionalElectionInContestUzwil = "1f144608-dcf7-40d5-b3fd-895560c4a91d";
    public const string ListIdKircheProportionalElectionInContestKirche = "3561571f-7b4c-469c-9e1b-65166e8f00f0";

    public const string ListUnion1IdGossauProportionalElectionInContestStGallen = "16892ba3-9b8c-42c7-914e-4b4692d170f4";
    public const string ListUnion2IdGossauProportionalElectionInContestStGallen = "6a8913a3-bd03-4cb3-a0f9-317db5de8959";
    public const string ListUnion3IdGossauProportionalElectionInContestStGallen = "687970fd-aeae-48d0-b291-7b6333912907";
    public const string SubListUnion11IdGossauProportionalElectionInContestStGallen = "5f53066c-1922-497d-a48b-cfd69579d892";
    public const string SubListUnion12IdGossauProportionalElectionInContestStGallen = "7fd14367-ff96-4ddc-89bc-47fb658527df";
    public const string SubListUnion21IdGossauProportionalElectionInContestStGallen = "49715fbf-5399-4981-bee5-01705469ec8c";
    public const string SubListUnion22IdGossauProportionalElectionInContestStGallen = "6a839c1a-c94a-4b5a-b59b-4c0edea82307";
    public const string ListUnionIdStGallenProportionalElectionInContestBund = "007ff21f-e61a-48f0-ab1f-6b3aa2c04c53";
    public const string ListUnionIdBundProportionalElectionInContestStGallen = "c0938e89-e5a4-4ee9-bd78-4ca972ddd68e";
    public const string ListUnionIdUzwilProportionalElectionInContestStGallen = "9d5cb38e-0a75-445f-970d-f97ae129f054";
    public const string ListUnionIdKircheProportionalElectionInContestKirche = "9cb1bb16-d284-427b-841c-6e04cea35b2d";
    public const string ListUnionIdGossauProportionalElectionInContestGossau = "0a8f4968-5546-4198-8c2f-b98b154fd0c6";

    public const string CandidateIdBundProportionalElectionInContestBund = "8ad43b77-2ef2-4241-bd66-8d87de236a74";
    public const string CandidateIdStGallenProportionalElectionInContestBund = "bba39596-a5f6-4729-a56f-e63871b30acc";
    public const string CandidateId1BundProportionalElectionInContestStGallen = "a31bf965-4824-4a05-a4fe-a43a7605b1f8";
    public const string CandidateId2BundProportionalElectionInContestStGallen = "7eaa113f-4273-4a06-b6b2-ee65919249d6";
    public const string CandidateIdUzwilProportionalElectionInContestStGallen = "d009d110-6269-4b6e-b9d1-84508de08d42";
    public const string CandidateIdStGallenProportionalElectionInContestStGallen = "9e131f21-4483-4375-b014-484c272615ee";
    public const string CandidateId1GossauProportionalElectionInContestStGallen = "8b4837a9-c3ba-4ec5-9e50-536a9b4347a9";
    public const string CandidateId2GossauProportionalElectionInContestStGallen = "9efe090f-883b-4e86-89c2-cd132ea84cbd";
    public const string CandidateId3GossauProportionalElectionInContestStGallen = "4933f558-bb34-4260-862a-fae71c52d619";
    public const string CandidateIdGossauProportionalElectionInContestGossau = "dd49aaba-ab8d-4eda-b83b-54beb8222af0";
    public const string CandidateIdUzwilProportionalElectionInContestUzwil = "7ddfff64-f55a-41ec-8ff5-0fde639a76c0";
    public const string CandidateId2UzwilProportionalElectionInContestUzwil = "24400876-69ab-4eb5-b3b6-192a40dcb8c7";
    public const string CandidateId3UzwilProportionalElectionInContestUzwil = "58f3c30c-f161-4ea7-bca7-35d78222fb52";
    public const string CandidateId11UzwilProportionalElectionInContestUzwil = "cbc9b619-971a-4ca9-9316-8bc291016233";
    public const string CandidateId12UzwilProportionalElectionInContestUzwil = "30b5efb6-8282-44bd-a3dc-98f87350239e";
    public const string CandidateIdKircheProportionalElectionInContestKirche = "e2000614-8633-4e35-b667-0eae6edc77e4";

    public static ProportionalElection BundProportionalElectionInContestBund
        => new ProportionalElection
        {
            Id = Guid.Parse(IdBundProportionalElectionInContestBund),
            PoliticalBusinessNumber = "100",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Bund",
                (t, s) => t.ShortDescription = s,
                "Pw Bund"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdBundProportionalElectionInContestBund),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, o) => t.Description = o,
                            "Liste 1X",
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdBundProportionalElectionInContestBund),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = AusmittlungUuidV5.BuildDomainOfInfluenceParty(
                                    Guid.Parse(ContestMockedData.IdBundesurnengang),
                                    Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundAndere)),
                            },
                        },
                    },
            },
        };

    public static ProportionalElection StGallenProportionalElectionInContestBund
        => new ProportionalElection
        {
            Id = Guid.Parse(IdStGallenProportionalElectionInContestBund),
            PoliticalBusinessNumber = "201",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl St. Gallen",
                (t, s) => t.ShortDescription = s,
                "Pw SG"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = false,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(List1IdStGallenProportionalElectionInContestBund),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdStGallenProportionalElectionInContestBund),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(List2IdStGallenProportionalElectionInContestBund),
                        Position = 2,
                        BlankRowCount = 1,
                        OrderNumber = "2a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 2"),
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdStGallenProportionalElectionInContestBund),
                        Position = 1,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Listenverbindung 1"),
                    },
            },
        };

    public static ProportionalElection BundProportionalElectionInContestStGallen
        => new ProportionalElection
        {
            Id = Guid.Parse(IdBundProportionalElectionInContestStGallen),
            PoliticalBusinessNumber = "100",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Bund",
                (t, s) => t.ShortDescription = s,
                "Pw Bund"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 0,
            AutomaticBallotBundleNumberGeneration = false,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdBundProportionalElectionInContestStGallen),
                        Position = 1,
                        BlankRowCount = 2,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId1BundProportionalElectionInContestStGallen),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = AusmittlungUuidV5.BuildDomainOfInfluenceParty(
                                    Guid.Parse(ContestMockedData.IdStGallenEvoting),
                                    Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundAndere)),
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId2BundProportionalElectionInContestStGallen),
                                FirstName = "firstName 2",
                                LastName = "lastName 2",
                                PoliticalFirstName = "pol first name 2",
                                PoliticalLastName = "pol last name 2",
                                DateOfBirth = new DateTime(1980, 3, 27, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 3,
                                Locality = "locality 2",
                                Number = "number2",
                                Sex = SexType.Undefined,
                                Title = "title2",
                                ZipCode = "zip code2",
                                Origin = "origin 2",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation 2",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title 2"),
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdBundProportionalElectionInContestStGallen),
                        Position = 1,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Listenverbindung 1"),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListIdBundProportionalElectionInContestStGallen),
                            },
                        },
                    },
            },
        };

    public static ProportionalElection UzwilProportionalElectionInContestStGallen
        => new ProportionalElection
        {
            Id = Guid.Parse(IdUzwilProportionalElectionInContestStGallen),
            PoliticalBusinessNumber = "166",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Uzwil",
                (t, s) => t.ShortDescription = s,
                "Pw Uzwil"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 10,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdUzwilProportionalElectionInContestStGallen),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdUzwilProportionalElectionInContestStGallen),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdUzwilProportionalElectionInContestStGallen),
                        Position = 1,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Listenverbindung 1"),
                    },
            },
        };

    public static ProportionalElection StGallenProportionalElectionInContestStGallen
        => new ProportionalElection
        {
            Id = Guid.Parse(IdStGallenProportionalElectionInContestStGallen),
            PoliticalBusinessNumber = "155",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl St. Gallen",
                (t, s) => t.ShortDescription = s,
                "Pw SG"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 50,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            CandidateCheckDigit = false,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdStGallenProportionalElectionInContestStGallen),
                        Position = 1,
                        BlankRowCount = 1,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdStGallenProportionalElectionInContestStGallen),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                        },
                    },
            },
        };

    public static ProportionalElection GossauProportionalElectionInContestStGallen
        => new ProportionalElection
        {
            Id = Guid.Parse(IdGossauProportionalElectionInContestStGallen),
            PoliticalBusinessNumber = "321",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Gossau",
                (t, s) => t.ShortDescription = s,
                "Pw Gossau"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 10,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 3,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId1GossauProportionalElectionInContestStGallen),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId2GossauProportionalElectionInContestStGallen),
                                FirstName = "candidate",
                                LastName = "number 2",
                                PoliticalFirstName = "pol first name 2",
                                PoliticalLastName = "pol last name 2",
                                DateOfBirth = new DateTime(1940, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 3,
                                Accumulated = false,
                                Locality = "locality 2",
                                Number = "number2",
                                Sex = SexType.Undefined,
                                Title = "title 2",
                                Origin = "origin 2",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation 2",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title 2"),
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                        Position = 2,
                        BlankRowCount = 0,
                        OrderNumber = "2",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 2"),
                        ProportionalElectionCandidates =
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId3GossauProportionalElectionInContestStGallen),
                                FirstName = "candidate",
                                LastName = "number 3",
                                PoliticalFirstName = "pol first name 3",
                                PoliticalLastName = "pol last name 3",
                                DateOfBirth = new DateTime(1940, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 1,
                                Accumulated = false,
                                Locality = "locality 3",
                                Number = "number3",
                                Sex = SexType.Undefined,
                                Title = "title 3",
                                Origin = "origin 3",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation 3",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title 3"),
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId3GossauProportionalElectionInContestStGallen),
                        Position = 3,
                        BlankRowCount = 3,
                        OrderNumber = "3a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 3"),
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnion1IdGossauProportionalElectionInContestStGallen),
                        Position = 1,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Listenverbindung 1"),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnion2IdGossauProportionalElectionInContestStGallen),
                        Position = 2,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Listenverbindung 2"),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId3GossauProportionalElectionInContestStGallen),
                            },
                        },
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnion3IdGossauProportionalElectionInContestStGallen),
                        Position = 3,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Listenverbindung 3"),
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnion11IdGossauProportionalElectionInContestStGallen),
                        Position = 1,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Unterlistenverbindung 1.1"),
                        ProportionalElectionRootListUnionId =
                            Guid.Parse(ListUnion1IdGossauProportionalElectionInContestStGallen),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnion12IdGossauProportionalElectionInContestStGallen),
                        Position = 2,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Unterlistenverbindung 1.2"),
                        ProportionalElectionRootListUnionId =
                            Guid.Parse(ListUnion1IdGossauProportionalElectionInContestStGallen),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnion21IdGossauProportionalElectionInContestStGallen),
                        Position = 1,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Unterlistenverbindung 2.1"),
                        ProportionalElectionRootListUnionId =
                            Guid.Parse(ListUnion2IdGossauProportionalElectionInContestStGallen),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId1GossauProportionalElectionInContestStGallen),
                    },
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(SubListUnion22IdGossauProportionalElectionInContestStGallen),
                        Position = 2,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Unterlistenverbindung 2.2"),
                        ProportionalElectionRootListUnionId =
                            Guid.Parse(ListUnion2IdGossauProportionalElectionInContestStGallen),
                        ProportionalElectionListUnionEntries = new List<ProportionalElectionListUnionEntry>
                        {
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId2GossauProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionListUnionEntry
                            {
                                ProportionalElectionListId =
                                    Guid.Parse(ListId3GossauProportionalElectionInContestStGallen),
                            },
                        },
                        ProportionalElectionMainListId = Guid.Parse(ListId3GossauProportionalElectionInContestStGallen),
                    },
            },
        };

    public static ProportionalElection StGallenProportionalElectionInContestStGallenWithoutChilds
        => new ProportionalElection
        {
            Id = Guid.Parse(IdStGallenProportionalElectionInContestStGallenWithoutChilds),
            PoliticalBusinessNumber = "500",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl St. Gallen 2",
                (t, s) => t.ShortDescription = s,
                "Pw SG2"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        };

    public static ProportionalElection GossauProportionalElectionInContestGossau
        => new ProportionalElection
        {
            Id = Guid.Parse(IdGossauProportionalElectionInContestGossau),
            PoliticalBusinessNumber = "324",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Gossau",
                (t, s) => t.ShortDescription = s,
                "Pw Gossau"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            ContestId = Guid.Parse(ContestMockedData.IdGossau),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 5,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdGossauProportionalElectionInContestGossau),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdGossauProportionalElectionInContestGossau),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                                PartyId = AusmittlungUuidV5.BuildDomainOfInfluenceParty(
                                    Guid.Parse(ContestMockedData.IdGossau),
                                    Guid.Parse(DomainOfInfluenceMockedData.PartyIdGossauFLiG)),
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdGossauProportionalElectionInContestGossau),
                        Position = 1,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Listenverbindung 1"),
                    },
            },
        };

    public static ProportionalElection UzwilProportionalElectionInContestUzwil
        => new ProportionalElection
        {
            Id = Guid.Parse(IdUzwilProportionalElectionInContestUzwilWithoutChilds),
            PoliticalBusinessNumber = "412",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Uzwil",
                (t, s) => t.ShortDescription = s,
                "Pw Uzwil"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdUzwilProportionalElectionInContestUzwil),
                        Position = 1,
                        BlankRowCount = 1,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdUzwilProportionalElectionInContestUzwil),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "1a.1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId2UzwilProportionalElectionInContestUzwil),
                                FirstName = "firstName2",
                                LastName = "lastName2",
                                PoliticalFirstName = "pol first name2",
                                PoliticalLastName = "pol last name2",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 3,
                                Accumulated = false,
                                Locality = "locality",
                                Number = "1a.2",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId3UzwilProportionalElectionInContestUzwil),
                                FirstName = "firstName3",
                                LastName = "lastName3",
                                PoliticalFirstName = "pol first name3",
                                PoliticalLastName = "pol last name3",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 4,
                                Accumulated = false,
                                Locality = "locality",
                                Number = "1a.3",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                        },
                    },
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListId2UzwilProportionalElectionInContestUzwil),
                        Position = 2,
                        BlankRowCount = 1,
                        OrderNumber = "2a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 2"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId11UzwilProportionalElectionInContestUzwil),
                                FirstName = "firstName11",
                                LastName = "lastName11",
                                PoliticalFirstName = "pol first name11",
                                PoliticalLastName = "pol last name11",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "2a.1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateId12UzwilProportionalElectionInContestUzwil),
                                FirstName = "firstName12",
                                LastName = "lastName12",
                                PoliticalFirstName = "pol first name12",
                                PoliticalLastName = "pol last name12",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 3,
                                Accumulated = true,
                                AccumulatedPosition = 4,
                                Locality = "locality",
                                Number = "2a.2",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                        },
                    },
            },
        };

    public static ProportionalElection UzwilProportionalElectionInContestBundWithoutChilds
        => new ProportionalElection
        {
            Id = Guid.Parse(IdUzwilProportionalElectionInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Uzwil",
                (t, s) => t.ShortDescription = s,
                "Pw Uzwil"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            Active = false,
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        };

    public static ProportionalElection GenfProportionalElectionInContestBundWithoutChilds
        => new ProportionalElection
        {
            Id = Guid.Parse(IdGenfProportionalElectionInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714a",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Genf",
                (t, s) => t.ShortDescription = s,
                "Pw Genf"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGenf),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        };

    public static ProportionalElection KircheProportionalElectionInContestKirche
        => new ProportionalElection
        {
            Id = Guid.Parse(IdKircheProportionalElectionInContestKirche),
            PoliticalBusinessNumber = "aaa",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Kirche",
                (t, s) => t.ShortDescription = s,
                "Pw Kirche"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdKirchgemeinde),
            ContestId = Guid.Parse(ContestMockedData.IdKirche),
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 4,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            ProportionalElectionLists = new List<ProportionalElectionList>
            {
                    new ProportionalElectionList
                    {
                        Id = Guid.Parse(ListIdKircheProportionalElectionInContestKirche),
                        Position = 1,
                        BlankRowCount = 0,
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                            (t, s) => t.ShortDescription = s,
                            "Liste 1"),
                        ProportionalElectionCandidates = new List<ProportionalElectionCandidate>
                        {
                            new ProportionalElectionCandidate
                            {
                                Id = Guid.Parse(CandidateIdKircheProportionalElectionInContestKirche),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1970, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 1,
                                Accumulated = true,
                                AccumulatedPosition = 2,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Male,
                                Title = "title",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<ProportionalElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title"),
                            },
                        },
                    },
            },
            ProportionalElectionListUnions = new List<ProportionalElectionListUnion>
            {
                    new ProportionalElectionListUnion
                    {
                        Id = Guid.Parse(ListUnionIdKircheProportionalElectionInContestKirche),
                        Position = 1,
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionListUnionTranslation>(
                            (t, o) => t.Description = o,
                            "Listenverbindung 1 Kirche"),
                    },
            },
        };

    public static ProportionalElection KircheProportionalElectionInContestKircheWithoutChilds
        => new ProportionalElection
        {
            Id = Guid.Parse(IdKircheProportionalElectionInContestKircheWithoutChilds),
            PoliticalBusinessNumber = "aaa",
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Proporzwahl Kirche ohne Listen",
                (t, s) => t.ShortDescription = s,
                "Pw Kirche ohne Listen"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdKirchgemeinde),
            ContestId = Guid.Parse(ContestMockedData.IdKirche),
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 2,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            CandidateCheckDigit = true,
            MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
            NumberOfMandates = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
        };

    public static IEnumerable<ProportionalElection> All
    {
        get
        {
            yield return BundProportionalElectionInContestBund;
            yield return BundProportionalElectionInContestStGallen;
            yield return UzwilProportionalElectionInContestStGallen;
            yield return StGallenProportionalElectionInContestBund;
            yield return StGallenProportionalElectionInContestStGallen;
            yield return GossauProportionalElectionInContestStGallen;
            yield return StGallenProportionalElectionInContestStGallenWithoutChilds;
            yield return GossauProportionalElectionInContestGossau;
            yield return UzwilProportionalElectionInContestUzwil;
            yield return UzwilProportionalElectionInContestBundWithoutChilds;
            yield return GenfProportionalElectionInContestBundWithoutChilds;
            yield return KircheProportionalElectionInContestKirche;
            yield return KircheProportionalElectionInContestKircheWithoutChilds;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        var proportionalElections = All.ToList();

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();

            foreach (var proportionalElection in proportionalElections)
            {
                var mappedDomainOfInfluence = await db.DomainOfInfluences.FirstAsync(doi =>
                    doi.SnapshotContestId == proportionalElection.ContestId && doi.BasisDomainOfInfluenceId == proportionalElection.DomainOfInfluenceId);
                proportionalElection.DomainOfInfluenceId = mappedDomainOfInfluence.Id;
            }

            db.ProportionalElections.AddRange(proportionalElections);
            await db.SaveChangesAsync();

            var proportionalElectionEndResultBuilder = sp.GetRequiredService<ProportionalElectionEndResultInitializer>();
            foreach (var proportionalElection in proportionalElections)
            {
                await proportionalElectionEndResultBuilder.RebuildForElection(proportionalElection.Id, ContestMockedData.TestingPhaseEnded(proportionalElection.ContestId));
            }
        });

        await runScoped(async sp =>
        {
            var builder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<ProportionalElection>>();
            foreach (var proportionalElection in proportionalElections)
            {
                await builder.Create(proportionalElection);
            }
        });

        await ProportionalElectionResultMockedData.Seed(runScoped, proportionalElections);
    }
}
