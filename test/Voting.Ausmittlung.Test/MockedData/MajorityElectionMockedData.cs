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

namespace Voting.Ausmittlung.Test.MockedData;

public static class MajorityElectionMockedData
{
    public const string IdBundMajorityElectionInContestBund = "7566c420-3774-4c57-9b31-9702fac37543";
    public const string IdStGallenMajorityElectionInContestBund = "b0da46f8-a721-4e1a-ac36-25284d68f34b";
    public const string IdUzwilMajorityElectionInContestBundWithoutChilds = "7ae77f44-1083-470a-bb66-64f921bc6945";
    public const string IdGossauMajorityElectionInContestStGallen = "50415df8-6ee9-4eb4-9e31-68c0d3021e76";
    public const string IdUzwilMajorityElectionInContestStGallen = "d66ced3e-a2e4-4178-932b-ac91ee6a9d85";
    public const string IdStGallenMajorityElectionInContestStGallen = "cd464c26-24d4-4cfc-95d9-e7c930b1784e";
    public const string IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot = "e966bec6-f1d4-4c07-8094-6f7df4cc515a";
    public const string IdStGallenMajorityElectionInContestStGallenWithoutChilds = "a6ab97b9-ce86-4973-876e-a128ff279bf7";
    public const string IdGossauMajorityElectionInContestGossau = "e39a4d1c-6db4-44a7-a707-05cf2005dd4a";
    public const string IdUzwilMajorityElectionInContestUzwilWithoutChilds = "4aebd757-9f88-4a76-90b4-497dc64adb6f";
    public const string IdGenfMajorityElectionInContestBundWithoutChilds = "2c3fe189-99a2-401a-8af6-8ac5f1bf3c3a";
    public const string IdKircheMajorityElectionInContestKirche = "65ec16ca-81cf-4c8d-9ee3-f741744c31fb";
    public const string IdKircheMajorityElectionInContestKircheWithoutChilds = "a24c7ec8-bca9-4c66-9030-dc2539fd1c06";

    public const string CandidateIdBundMajorityElectionInContestBund = "94a02a0c-b654-4917-92a0-f6fe3fa05799";
    public const string CandidateId1StGallenMajorityElectionInContestBund = "81a11b8e-51b8-40c5-aa94-b7a854e2c726";
    public const string CandidateId2StGallenMajorityElectionInContestBund = "efdbb5e3-16bf-4a53-95c3-a35ed6371819";
    public const string CandidateIdUzwilMajorityElectionInContestStGallen = "be81a3f3-5a9e-4a69-8f19-4f0598a32955";
    public const string CandidateIdStGallenMajorityElectionInContestStGallen = "1228f95d-8b39-44b1-8cc3-84a93f5e3bbc";
    public const string CandidateIdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot = "a41d3778-f9e6-4447-ae59-46ea77419efd";
    public const string CandidateIdReferencedStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot = "d849b362-9ae0-4a2e-a10a-033a25ed4075";
    public const string CandidateId1GossauMajorityElectionInContestStGallen = "77ce6bf0-b27c-4d9d-926f-ced7863aff2f";
    public const string CandidateId2GossauMajorityElectionInContestStGallen = "4a44cb35-05a7-41f9-aa1e-034bedd320ec";
    public const string CandidateIdGossauMajorityElectionInContestGossau = "194ff485-6eb9-4d98-8bec-855a2ec92650";
    public const string CandidateIdUzwilMajorityElectionInContestUzwil = "3be5ce95-56db-424d-ab11-6fbb18196862";
    public const string CandidateIdKircheMajorityElectionInContestKirche = "56cd0f70-9976-4efb-b09d-d6e60fa03904";

    public const string SecondaryElectionIdStGallenMajorityElectionInContestBund = "0741da26-add2-4c4c-960d-7e251b82e91b";
    public const string SecondaryElectionIdStGallenMajorityElectionInContestBund2 = "292a5a89-e030-44db-a204-90e00540ba84";
    public const string SecondaryElectionIdStGallenMajorityElectionInContestBund3 = "f4ae2b39-45b5-427a-96e4-24d645ddb080";
    public const string SecondaryElectionIdUzwilMajorityElectionInContestStGallen = "5ec8cf85-229a-4dc7-a601-1bb6980fee78";
    public const string SecondaryElectionIdKircheMajorityElectionInContestKirche = "12a6adbe-8872-4baa-982d-a4a0dae93834";

    public const string SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund = "9d1e4ef4-81c0-4905-a2c6-e1b937b80ddf";
    public const string SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund = "f70ffba1-1cfd-402b-b6ea-f2c9f802886a";
    public const string SecondaryElectionCandidateId3StGallenMajorityElectionInContestBund = "fdb57357-095d-469e-8f40-fd3d0b8817cb";
    public const string SecondaryElectionCandidateId4StGallenMajorityElectionInContestBund = "340c482b-c064-427e-a906-2696fab2ab35";
    public const string SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund2 = "4076daaa-b791-4923-94dc-498ad171d062";
    public const string SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund2 = "910181c5-c003-4775-976b-1f33a19f2a1b";
    public const string SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund3 = "f136f4d7-7f4c-4674-a9c5-391dbed43985";
    public const string SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund3 = "d7b2bd15-6ce9-4dbe-a56f-72c3fab598b3";
    public const string SecondaryElectionCandidateIdUzwilMajorityElectionInContestStGallen = "6bf76024-ed15-4b7e-b68e-583b7bb5d2d5";
    public const string SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche = "30e50602-ad94-40bf-b8ff-0f91c82d20b1";

    public const string ElectionGroupIdStGallenMajorityElectionInContestBund = "63bf2387-08e0-45ed-96e6-263f85500e28";
    public const string ElectionGroupIdUzwilMajorityElectionInContestStGallen = "6eaa4d6b-eb0a-4315-b0b8-a68266303ab1";
    public const string ElectionGroupIdKircheMajorityElectionInContestKirche = "2db8164f-ae7a-40d2-a5e3-5aba48bcc70a";

    public const string BallotGroupIdStGallenMajorityElectionInContestBund = "7a32239e-1cd1-4deb-9ecb-f5f2aa2f9949";
    public const string BallotGroupIdUzwilMajorityElectionInContestStGallen = "0034f358-1baa-47b4-a669-e648b5493f1e";
    public const string BallotGroupIdKircheMajorityElectionInContestKirche = "722a3a08-dede-4366-be30-fcb7c08cc010";

    public const string BallotGroupEntryId1StGallenMajorityElectionInContestBund = "513f5663-13f1-463b-b24d-4b3a7e2f7446";
    public const string BallotGroupEntryId2StGallenMajorityElectionInContestBund = "b0a01fbd-ea47-4228-91d4-8f3054d5fe93";
    public const string BallotGroupEntryId3StGallenMajorityElectionInContestBund = "79c71f2e-7e8b-4b57-8e64-de65a90f454e";
    public const string BallotGroupEntryId4StGallenMajorityElectionInContestBund = "09ecea4c-5483-483b-90e6-a86165277c99";

    public static MajorityElection BundMajorityElectionInContestBund
        => new MajorityElection
        {
            Id = Guid.Parse(IdBundMajorityElectionInContestBund),
            PoliticalBusinessNumber = "100",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Bund",
                (t, s) => t.ShortDescription = s,
                "Mw Bund"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 0,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdBundMajorityElectionInContestBund),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "CVP"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
        };

    public static MajorityElection StGallenMajorityElectionInContestBund
        => new MajorityElection
        {
            Id = Guid.Parse(IdStGallenMajorityElectionInContestBund),
            PoliticalBusinessNumber = "201",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl St. Gallen",
                (t, s) => t.ShortDescription = s,
                "Mw SG"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = false,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
            NumberOfMandates = 1,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId1StGallenMajorityElectionInContestBund),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "Test"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId2StGallenMajorityElectionInContestBund),
                        FirstName = "firstName2",
                        LastName = "lastName2",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = false,
                        Position = 2,
                        Locality = "locality",
                        Number = "number5",
                        Sex = SexType.Male,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation2",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title2",
                            (t, s) => t.Party = s,
                            "Test"),
                        CheckDigit = 1,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
            {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdStGallenMajorityElectionInContestBund),
                        Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionTranslation>(
                            (t, o) => t.OfficialDescription = o,
                            "official",
                            (t, s) => t.ShortDescription = s,
                            "short"),
                        NumberOfMandates = 3,
                        PoliticalBusinessNumber = "n1",
                        ElectionGroupId = Guid.Parse(ElectionGroupIdStGallenMajorityElectionInContestBund),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Undefined,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                CandidateReferenceId = Guid.Parse(CandidateId1StGallenMajorityElectionInContestBund),
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "Test"),
                                CheckDigit = 9,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund),
                                FirstName = "Peter",
                                LastName = "Lustig",
                                PoliticalFirstName = "Pete",
                                PoliticalLastName = "L",
                                DateOfBirth = new DateTime(1982, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 2,
                                Locality = "locality",
                                Number = "number2",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "Beruf",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "CVP"),
                                CheckDigit = 7,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId3StGallenMajorityElectionInContestBund),
                                FirstName = "Hansruedi",
                                LastName = "Proll",
                                PoliticalFirstName = "Hansi",
                                PoliticalLastName = "Proll",
                                DateOfBirth = new DateTime(1972, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 3,
                                Locality = "locality",
                                Number = "number3",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "Beruf3",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title 3",
                                    (t, s) => t.Party = s,
                                    "SVP"),
                                CheckDigit = 5,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId4StGallenMajorityElectionInContestBund),
                                FirstName = "Susanne",
                                LastName = "Meierhans",
                                PoliticalFirstName = "Susi",
                                PoliticalLastName = "Meierhans",
                                DateOfBirth = new DateTime(1992, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 4,
                                Locality = "locality",
                                Number = "number4",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "Beruf",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "CVP"),
                                CheckDigit = 3,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdStGallenMajorityElectionInContestBund2),
                        Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionTranslation>(
                            (t, o) => t.OfficialDescription = o,
                            "official2",
                            (t, s) => t.ShortDescription = s,
                            "short2"),
                        NumberOfMandates = 1,
                        PoliticalBusinessNumber = "n2",
                        ElectionGroupId = Guid.Parse(ElectionGroupIdStGallenMajorityElectionInContestBund),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund2),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                CandidateReferenceId = Guid.Parse(CandidateId1StGallenMajorityElectionInContestBund),
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "Test"),
                                CheckDigit = 9,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund2),
                                FirstName = "Peter2",
                                LastName = "Lustig2",
                                PoliticalFirstName = "Pete2",
                                PoliticalLastName = "L",
                                DateOfBirth = new DateTime(1982, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 2,
                                Locality = "locality",
                                Number = "number2",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "Beruf",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "CVP"),
                                CheckDigit = 7,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdStGallenMajorityElectionInContestBund3),
                        Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionTranslation>(
                            (t, o) => t.OfficialDescription = o,
                            "official3",
                            (t, s) => t.ShortDescription = s,
                            "short3"),
                        NumberOfMandates = 2,
                        PoliticalBusinessNumber = "n3",
                        ElectionGroupId = Guid.Parse(ElectionGroupIdStGallenMajorityElectionInContestBund),
                        Active = true,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund3),
                                FirstName = "firstName",
                                LastName = "lastName",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Female,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                CandidateReferenceId = Guid.Parse(CandidateId1StGallenMajorityElectionInContestBund),
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "Test"),
                                CheckDigit = 9,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund3),
                                FirstName = "Peter2",
                                LastName = "Lustig2",
                                PoliticalFirstName = "Pete2",
                                PoliticalLastName = "L",
                                DateOfBirth = new DateTime(1982, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 2,
                                Locality = "locality",
                                Number = "number2",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "Beruf",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "CVP"),
                                CheckDigit = 7,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
            },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupIdStGallenMajorityElectionInContestBund),
                Description = "Test Election Group",
                Number = 1,
            },
            BallotGroups = new List<MajorityElectionBallotGroup>
            {
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupIdStGallenMajorityElectionInContestBund),
                        Description = "BG1 long description",
                        ShortDescription = "BG1",
                        Position = 1,
                        AllCandidateCountsOk = true,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId1StGallenMajorityElectionInContestBund),
                                PrimaryMajorityElectionId = Guid.Parse(IdStGallenMajorityElectionInContestBund),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("5ce4ebba-554e-4e8d-b603-b36155d11af5"),
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateId1StGallenMajorityElectionInContestBund),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId2StGallenMajorityElectionInContestBund),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdStGallenMajorityElectionInContestBund),
                                BlankRowCount = 2,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("283164ab-eb50-4035-8602-9d49d1ff1a51"),
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId3StGallenMajorityElectionInContestBund),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdStGallenMajorityElectionInContestBund2),
                                IndividualCandidatesVoteCount = 1,
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupEntryId4StGallenMajorityElectionInContestBund),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdStGallenMajorityElectionInContestBund3),
                                BlankRowCount = 2,
                            },
                        },
                    },
            },
        };

    public static MajorityElection UzwilMajorityElectionInContestStGallen
        => new MajorityElection
        {
            Id = Guid.Parse(IdUzwilMajorityElectionInContestStGallen),
            PoliticalBusinessNumber = "166",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Uzwil",
                (t, s) => t.ShortDescription = s,
                "Mw Uzwil"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 5,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdUzwilMajorityElectionInContestStGallen),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "FDP"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
            {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdUzwilMajorityElectionInContestStGallen),
                        Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionTranslation>(
                            (t, o) => t.OfficialDescription = o,
                            "official",
                            (t, s) => t.ShortDescription = s,
                            "short"),
                        NumberOfMandates = 2,
                        PoliticalBusinessNumber = "n1",
                        ElectionGroupId = Guid.Parse(ElectionGroupIdUzwilMajorityElectionInContestStGallen),
                        Active = true,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateIdUzwilMajorityElectionInContestStGallen),
                                FirstName = "first",
                                LastName = "last",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1960, 2, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = true,
                                Position = 1,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                CandidateReferenceId = Guid.Parse(CandidateIdUzwilMajorityElectionInContestStGallen),
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "Test"),
                                CheckDigit = 9,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
            },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupIdUzwilMajorityElectionInContestStGallen),
                Description = "Test Election Group",
                Number = 1,
            },
            BallotGroups = new List<MajorityElectionBallotGroup>
            {
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupIdUzwilMajorityElectionInContestStGallen),
                        Description = "BG1 long description",
                        ShortDescription = "BG1",
                        Position = 1,
                        AllCandidateCountsOk = true,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse("73d86ebb-4732-487b-bc4b-7b8a82e08ddb"),
                                PrimaryMajorityElectionId = Guid.Parse(IdUzwilMajorityElectionInContestStGallen),
                                BlankRowCount = 3,
                                IndividualCandidatesVoteCount = 1,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("e4a4adea-e9e8-45a6-bb16-5015934d87e7"),
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateIdUzwilMajorityElectionInContestStGallen),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse("91e4d730-41a7-4b8c-ad7f-2647c03af8c8"),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdUzwilMajorityElectionInContestStGallen),
                                BlankRowCount = 0,
                                IndividualCandidatesVoteCount = 1,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("53e7c8f3-330f-4d5e-847e-046ee1e89372"),
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryElectionCandidateIdUzwilMajorityElectionInContestStGallen),
                                    },
                                },
                            },
                        },
                    },
            },
        };

    public static MajorityElection StGallenMajorityElectionInContestStGallen
        => new MajorityElection
        {
            Id = Guid.Parse(IdStGallenMajorityElectionInContestStGallen),
            PoliticalBusinessNumber = "155",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl St. Gallen",
                (t, s) => t.ShortDescription = s,
                "Mw SG"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 50,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = false,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdStGallenMajorityElectionInContestStGallen),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "GLP"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
        };

    public static MajorityElection StGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot
        => new MajorityElection
        {
            Id = Guid.Parse(IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot),
            PoliticalBusinessNumber = "155.1",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Nebenwahl St. Gallen",
                (t, s) => t.ShortDescription = s,
                "NW SG"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 50,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = false,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.RelativeMajority,
            NumberOfMandates = 1,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            PrimaryMajorityElectionId = Guid.Parse(IdStGallenMajorityElectionInContestStGallen),
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdReferencedStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "GLP"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                        CandidateReferenceId = Guid.Parse(CandidateIdStGallenMajorityElectionInContestStGallen),
                    },
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot),
                        FirstName = "firstName2",
                        LastName = "lastName2",
                        PoliticalFirstName = "pol first name2",
                        PoliticalLastName = "pol last name2",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "SVP"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
        };

    public static MajorityElection GossauMajorityElectionInContestStGallen
        => new MajorityElection
        {
            Id = Guid.Parse(IdGossauMajorityElectionInContestStGallen),
            PoliticalBusinessNumber = "322",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Gossau",
                (t, s) => t.ShortDescription = s,
                "Mw Gossau"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 10,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 3,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId1GossauMajorityElectionInContestStGallen),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "CVP"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateId2GossauMajorityElectionInContestStGallen),
                        FirstName = "candidate",
                        LastName = "number 2",
                        PoliticalFirstName = "pol first name 2",
                        PoliticalLastName = "pol last name 2",
                        DateOfBirth = new DateTime(1940, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = false,
                        Position = 2,
                        Locality = "locality 2",
                        Number = "number2",
                        Sex = SexType.Undefined,
                        Title = "title 2",
                        Origin = "origin 2",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation 2",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title 2",
                            (t, s) => t.Party = s,
                            "CVP"),
                        CheckDigit = 7,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
        };

    public static MajorityElection StGallenMajorityElectionInContestStGallenWithoutChilds
        => new MajorityElection
        {
            Id = Guid.Parse(IdStGallenMajorityElectionInContestStGallenWithoutChilds),
            PoliticalBusinessNumber = "500",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl St. Gallen 2",
                (t, s) => t.ShortDescription = s,
                "Mw SG2"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
        };

    public static MajorityElection GossauMajorityElectionInContestGossau
        => new MajorityElection
        {
            Id = Guid.Parse(IdGossauMajorityElectionInContestGossau),
            PoliticalBusinessNumber = "401",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Gossau",
                (t, s) => t.ShortDescription = s,
                "Mw Gossau"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            ContestId = Guid.Parse(ContestMockedData.IdGossau),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 20,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdGossauMajorityElectionInContestGossau),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "Test"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
        };

    public static MajorityElection UzwilMajorityElectionInContestUzwil
        => new MajorityElection
        {
            Id = Guid.Parse(IdUzwilMajorityElectionInContestUzwilWithoutChilds),
            PoliticalBusinessNumber = "412",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Uzwil",
                (t, s) => t.ShortDescription = s,
                "Mw Uzwil"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            ContestId = Guid.Parse(ContestMockedData.IdUzwilEVoting),
            Active = true,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdUzwilMajorityElectionInContestUzwil),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = true,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Female,
                        Title = "title",
                        ZipCode = "zip code",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "None"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
        };

    public static MajorityElection UzwilMajorityElectionInContestBundWithoutChilds
        => new MajorityElection
        {
            Id = Guid.Parse(IdUzwilMajorityElectionInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Uzwil",
                (t, s) => t.ShortDescription = s,
                "Mw Uzwil"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            Active = false,
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 2,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
        };

    public static MajorityElection GenfMajorityElectionInContestBundWithoutChilds
        => new MajorityElection
        {
            Id = Guid.Parse(IdGenfMajorityElectionInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714a",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Genf",
                (t, s) => t.ShortDescription = s,
                "Mw Genf"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGenf),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 1,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = true,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
        };

    public static MajorityElection KircheMajorityElectionInContestKirche
        => new MajorityElection
        {
            Id = Guid.Parse(IdKircheMajorityElectionInContestKirche),
            PoliticalBusinessNumber = "aaa",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Kirche",
                (t, s) => t.ShortDescription = s,
                "Mw Kirche"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdKirchgemeinde),
            ContestId = Guid.Parse(ContestMockedData.IdKirche),
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.Detailed,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = false,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
            {
                    new MajorityElectionCandidate
                    {
                        Id = Guid.Parse(CandidateIdKircheMajorityElectionInContestKirche),
                        FirstName = "firstName",
                        LastName = "lastName",
                        PoliticalFirstName = "pol first name",
                        PoliticalLastName = "pol last name",
                        DateOfBirth = new DateTime(1970, 1, 13, 0, 0, 0, DateTimeKind.Utc),
                        Incumbent = false,
                        Position = 1,
                        Locality = "locality",
                        Number = "number1",
                        Sex = SexType.Male,
                        Title = "title",
                        Origin = "origin",
                        Translations = TranslationUtil.CreateTranslations<MajorityElectionCandidateTranslation>(
                            (t, o) => t.Occupation = o,
                            "occupation",
                            (t, o) => t.OccupationTitle = o,
                            "occupation title",
                            (t, s) => t.Party = s,
                            "test"),
                        CheckDigit = 9,
                        Street = "street",
                        HouseNumber = "1a",
                        Country = "CH",
                    },
            },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
            {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionIdKircheMajorityElectionInContestKirche),
                        Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionTranslation>(
                            (t, o) => t.OfficialDescription = o,
                            "official",
                            (t, s) => t.ShortDescription = s,
                            "short"),
                        NumberOfMandates = 2,
                        PoliticalBusinessNumber = "n1",
                        ElectionGroupId = Guid.Parse(ElectionGroupIdKircheMajorityElectionInContestKirche),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            new SecondaryMajorityElectionCandidate
                            {
                                Id = Guid.Parse(SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche),
                                FirstName = "first",
                                LastName = "last",
                                PoliticalFirstName = "pol first name",
                                PoliticalLastName = "pol last name",
                                DateOfBirth = new DateTime(1980, 12, 13, 0, 0, 0, DateTimeKind.Utc),
                                Incumbent = false,
                                Position = 1,
                                Locality = "locality",
                                Number = "number1",
                                Sex = SexType.Male,
                                Title = "title",
                                ZipCode = "zip code",
                                Origin = "origin",
                                Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                                    (t, o) => t.Occupation = o,
                                    "occupation",
                                    (t, o) => t.OccupationTitle = o,
                                    "occupation title",
                                    (t, s) => t.Party = s,
                                    "Test"),
                                CheckDigit = 9,
                                Street = "street",
                                HouseNumber = "1a",
                                Country = "CH",
                            },
                        },
                    },
            },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupIdKircheMajorityElectionInContestKirche),
                Description = "Test Election Group",
                Number = 1,
            },
            BallotGroups = new List<MajorityElectionBallotGroup>
            {
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupIdKircheMajorityElectionInContestKirche),
                        Description = "BG1 long description",
                        ShortDescription = "BG1",
                        Position = 1,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse("36e30984-9949-452a-b949-d384966680f1"),
                                PrimaryMajorityElectionId = Guid.Parse(IdKircheMajorityElectionInContestKirche),
                                BlankRowCount = 0,
                                IndividualCandidatesVoteCount = 1,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("16aedf82-87db-4fac-b941-4d5b22d48838"),
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateIdKircheMajorityElectionInContestKirche),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse("5539d962-2cbe-4e5e-ab55-eabf2866aefa"),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionIdKircheMajorityElectionInContestKirche),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        Id = Guid.Parse("46cd92ef-1837-42c1-a828-a9df02b1cbb5"),
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche),
                                    },
                                },
                            },
                        },
                    },
            },
        };

    public static MajorityElection KircheMajorityElectionInContestKircheWithoutChilds
        => new MajorityElection
        {
            Id = Guid.Parse(IdKircheMajorityElectionInContestKircheWithoutChilds),
            PoliticalBusinessNumber = "aaa",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl Kirche ohne Listen",
                (t, s) => t.ShortDescription = s,
                "Mw Kirche ohne Listen"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdKirchgemeinde),
            ContestId = Guid.Parse(ContestMockedData.IdKirche),
            Active = false,
            AutomaticEmptyVoteCounting = true,
            EnforceEmptyVoteCountingForCountingCircles = true,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 10,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = true,
            MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
            NumberOfMandates = 5,
            ReviewProcedure = MajorityElectionReviewProcedure.Physically,
            EnforceReviewProcedureForCountingCircles = false,
            EnforceCandidateCheckDigitForCountingCircles = false,
        };

    public static IEnumerable<MajorityElection> All
    {
        get
        {
            yield return BundMajorityElectionInContestBund;
            yield return UzwilMajorityElectionInContestStGallen;
            yield return StGallenMajorityElectionInContestBund;
            yield return StGallenMajorityElectionInContestStGallen;
            yield return StGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot;
            yield return GossauMajorityElectionInContestStGallen;
            yield return StGallenMajorityElectionInContestStGallenWithoutChilds;
            yield return GossauMajorityElectionInContestGossau;
            yield return UzwilMajorityElectionInContestUzwil;
            yield return UzwilMajorityElectionInContestBundWithoutChilds;
            yield return GenfMajorityElectionInContestBundWithoutChilds;
            yield return KircheMajorityElectionInContestKirche;
            yield return KircheMajorityElectionInContestKircheWithoutChilds;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, bool onlyDetailed = false)
    {
        var majorityElections = All.ToList();

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();

            foreach (var majorityElection in majorityElections)
            {
                var mappedDomainOfInfluence = await db.DomainOfInfluences.FirstAsync(doi =>
                    doi.SnapshotContestId == majorityElection.ContestId && doi.BasisDomainOfInfluenceId == majorityElection.DomainOfInfluenceId);
                majorityElection.DomainOfInfluenceId = mappedDomainOfInfluence.Id;
            }

            db.MajorityElections.AddRange(majorityElections);
            await db.SaveChangesAsync();

            var majorityElectionEndResultBuilder = sp.GetRequiredService<MajorityElectionEndResultInitializer>();
            foreach (var majorityElection in majorityElections)
            {
                await majorityElectionEndResultBuilder.RebuildForElection(majorityElection.Id, ContestMockedData.TestingPhaseEnded(majorityElection.ContestId));
            }
        });

        // seed primary simple political businesses
        await runScoped(async sp =>
        {
            var builder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<MajorityElection>>();
            foreach (var majorityElection in majorityElections)
            {
                await builder.Create(majorityElection);
            }
        });

        // seed secondary simple political businesses
        var secondaryMajorityElections = majorityElections.SelectMany(x => x.SecondaryMajorityElections).ToList();
        await runScoped(async sp =>
        {
            var builder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<SecondaryMajorityElection>>();
            foreach (var secondaryMajorityElection in secondaryMajorityElections)
            {
                await builder.Create(secondaryMajorityElection);
                await builder.AdjustCountOfSecondaryBusinesses(secondaryMajorityElection.PrimaryMajorityElectionId, 1);
            }
        });

        await MajorityElectionResultMockedData.Seed(runScoped, majorityElections, onlyDetailed);
    }
}
