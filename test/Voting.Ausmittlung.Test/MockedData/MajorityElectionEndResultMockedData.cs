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
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.Utils;

namespace Voting.Ausmittlung.Test.MockedData;

public static class MajorityElectionEndResultMockedData
{
    public const string ElectionId = "7a7f337e-78c8-44f4-83eb-507d383fc8bf";

    public const string CandidateId1 = "6818fc94-2eb7-43aa-9406-778e81eee013";
    public const string CandidateId2 = "a9df7271-04d8-4bea-b654-499de0a99942";
    public const string CandidateId3 = "b5e78bac-1f72-4699-bcb5-e132fe25f94e";
    public const string CandidateId4 = "bf683ec2-27c1-44b9-b56b-6e4c82e4dd60";
    public const string CandidateId5 = "5e0e84f0-4164-4201-99a2-521528203ceb";
    public const string CandidateId6 = "48df4e61-6016-40fb-b718-6ce8a3bbac85";
    public const string CandidateId7 = "9457a86d-d004-49f2-95e6-a9f3b8182d8f";
    public const string CandidateId8 = "cd354462-45fe-44b5-b0e5-c68ba98d4b97";
    public const string CandidateId9InBallotGroup = "db3f7951-aefb-4b91-9d4c-1b6fc43b28e4";
    public const string CandidateId10InBallotGroup = "3726f09b-2c01-420e-8f17-4010d065cfcf";

    public const string SecondaryElectionId = "8cfaec6d-6e37-472e-b63e-073f6fd980fe";
    public const string SecondaryElectionId2 = "f78a229e-0790-4e69-b212-7c5fd960cd98";

    public const string SecondaryCandidateId1 = "65f99dab-eb37-461f-9717-9d425d10732c";
    public const string SecondaryCandidateId2 = "bb79e109-3b25-41d2-af1d-c7e4468c24e8";
    public const string SecondaryCandidateId3 = "5ee6f477-660a-4832-9bd0-d4f6c5cd91ee";
    public const string SecondaryCandidateId4InBallotGroup = "e9d3565b-ebac-4ad2-9297-33cd38f074bb";
    public const string Secondary2CandidateId1 = "6cc4eccf-c548-4362-96cb-c8e45e9a037e";
    public const string Secondary2CandidateId2 = "c2b6af61-0e09-4f27-85a9-2305f24d48b2";
    public const string Secondary2CandidateId3 = "5e8da5c9-7bd9-4389-b23a-d9dd67aa1b33";
    public const string Secondary2CandidateId4 = "6e85bf5a-6df2-48c4-84a6-bb3fa7a7401f";

    public const string ElectionGroupId = "aeb7c0b6-7697-48c4-b5f5-e0720a1c15fc";
    public const string BallotGroupId = "7eeacf7e-298b-49f3-9c8f-2b923c8b0b3b";

    public const string BallotGroupPrimaryElectionEntryId = "55dcb32b-2f6c-40c4-9113-442b619a44e4";
    public const string BallotGroupSecondaryElectionEntryId = "54074444-4341-4ac9-91e0-4459a4e951bc";

    public static readonly Guid ElectionGuid = Guid.Parse(ElectionId);

    public static readonly Guid StGallenResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidStGallen, false);
    public static readonly Guid StGallenAuslandschweizerResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidStGallenAuslandschweizer, false);
    public static readonly Guid StGallenStFidenResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidStGallenStFiden, false);
    public static readonly Guid StGallenHaggenResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidStGallenHaggen, false);
    public static readonly Guid GossauResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidGossau, false);
    public static readonly Guid UzwilResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ElectionGuid, CountingCircleMockedData.GuidUzwil, false);

    public static readonly string StGallenResultId = StGallenResultGuid.ToString();
    public static readonly string StGallenAuslandschweizerResultId = StGallenAuslandschweizerResultGuid.ToString();
    public static readonly string StGallenStFidenResultId = StGallenStFidenResultGuid.ToString();
    public static readonly string StGallenHaggenResultId = StGallenHaggenResultGuid.ToString();
    public static readonly string GossauResultId = GossauResultGuid.ToString();
    public static readonly string UzwilResultId = UzwilResultGuid.ToString();

    public static async Task Seed(
        Func<Func<IServiceProvider, Task>, Task> runScoped,
        MajorityElectionMandateAlgorithm alg = MajorityElectionMandateAlgorithm.AbsoluteMajority,
        int primaryElectionNumberOfMandates = 3)
    {
        var election = BuildElection(
            MajorityElectionResultEntry.FinalResults,
            alg,
            primaryElectionNumberOfMandates,
            1,
            2);
        var electionId = Guid.Parse(ElectionId);

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var simplePbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<MajorityElection>>();

            var mappedDomainOfInfluence = await db.DomainOfInfluences.FirstAsync(doi =>
                doi.SnapshotContestId == election.ContestId && doi.BasisDomainOfInfluenceId == election.DomainOfInfluenceId);
            election.DomainOfInfluenceId = mappedDomainOfInfluence.Id;

            db.MajorityElections.Add(election);
            await db.SaveChangesAsync();

            await simplePbBuilder.Create(election);

            await sp.GetRequiredService<MajorityElectionResultBuilder>()
                .RebuildForElection(electionId, mappedDomainOfInfluence.Id, false, election.ContestId);

            var endResultInitializer = sp.GetRequiredService<MajorityElectionEndResultInitializer>();
            await endResultInitializer.RebuildForElection(electionId, false);

            var results = await db.MajorityElectionResults
                .AsTracking()
                .AsSplitQuery()
                .Where(x => x.MajorityElectionId == electionId)
                .Include(x => x.CandidateResults)
                .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                .ToListAsync();
            SetResultsMockData(results);
            await db.SaveChangesAsync();

            var endResultBuilder = sp.GetRequiredService<MajorityElectionEndResultBuilder>();
            foreach (var result in results)
            {
                await endResultBuilder.AdjustEndResult(result.Id, false);
            }

            await db.SaveChangesAsync();
        });
    }

    public static MajorityElection BuildElection(
        MajorityElectionResultEntry resultEntry,
        MajorityElectionMandateAlgorithm mandateAlgorithm,
        int primaryElectionNumberOfMandates,
        int secondaryElectionNumberOfMandates = 1,
        int secondaryElectionNumberOfMandates2 = 1)
    {
        return new MajorityElection
        {
            Id = Guid.Parse(ElectionId),
            PoliticalBusinessNumber = "201",
            Translations = TranslationUtil.CreateTranslations<MajorityElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Majorzwahl",
                (t, s) => t.ShortDescription = s,
                "Majorzw"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            AutomaticEmptyVoteCounting = false,
            EnforceEmptyVoteCountingForCountingCircles = false,
            BallotBundleSize = 25,
            BallotBundleSampleSize = 3,
            AutomaticBallotBundleNumberGeneration = true,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            ResultEntry = resultEntry,
            EnforceResultEntryForCountingCircles = false,
            CandidateCheckDigit = false,
            MandateAlgorithm = mandateAlgorithm,
            NumberOfMandates = primaryElectionNumberOfMandates,
            ReportDomainOfInfluenceLevel = 1,
            ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = true,
            EnforceCandidateCheckDigitForCountingCircles = true,
            FederalIdentification = 333,
            MajorityElectionCandidates = new List<MajorityElectionCandidate>
                {
                    BuildCandidate(CandidateId1, 1, 9),
                    BuildCandidate(CandidateId2, 2, 7),
                    BuildCandidate(CandidateId3, 3, 5),
                    BuildCandidate(CandidateId4, 4, 3),
                    BuildCandidate(CandidateId5, 5, 1),
                    BuildCandidate(CandidateId6, 6, 0),
                    BuildCandidate(CandidateId7, 7, 8),
                    BuildCandidate(CandidateId8, 8, 6),
                    BuildCandidate(CandidateId9InBallotGroup, 9, 4),
                    BuildCandidate(CandidateId10InBallotGroup, 10, 8),
                },
            SecondaryMajorityElections = new List<SecondaryMajorityElection>
                {
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionId),
                        Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionTranslation>(
                            (t, o) => t.OfficialDescription = o,
                            "official",
                            (t, s) => t.ShortDescription = s,
                            "short"),
                        NumberOfMandates = secondaryElectionNumberOfMandates,
                        PoliticalBusinessNumber = "n1",
                        AllowedCandidates = SecondaryMajorityElectionAllowedCandidate.MayExistInPrimaryElection,
                        ElectionGroupId = Guid.Parse(ElectionGroupId),
                        Active = false,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            BuildSecondaryCandidate(SecondaryCandidateId1, 1, 9, CandidateId1),
                            BuildSecondaryCandidate(SecondaryCandidateId2, 2, 7),
                            BuildSecondaryCandidate(SecondaryCandidateId3, 3, 5),
                            BuildSecondaryCandidate(SecondaryCandidateId4InBallotGroup, 4, 3),
                        },
                    },
                    new SecondaryMajorityElection
                    {
                        Id = Guid.Parse(SecondaryElectionId2),
                        Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionTranslation>(
                            (t, o) => t.OfficialDescription = o,
                            "official2",
                            (t, s) => t.ShortDescription = s,
                            "short2"),
                        NumberOfMandates = secondaryElectionNumberOfMandates2,
                        PoliticalBusinessNumber = "n2",
                        AllowedCandidates = SecondaryMajorityElectionAllowedCandidate.MayExistInPrimaryElection,
                        ElectionGroupId = Guid.Parse(ElectionGroupId),
                        Active = true,
                        Candidates = new List<SecondaryMajorityElectionCandidate>
                        {
                            BuildSecondaryCandidate(Secondary2CandidateId1, 1, 9),
                            BuildSecondaryCandidate(Secondary2CandidateId2, 2, 7),
                            BuildSecondaryCandidate(Secondary2CandidateId3, 3, 5, CandidateId1),
                            BuildSecondaryCandidate(Secondary2CandidateId4, 4, 3),
                        },
                    },
                },
            ElectionGroup = new ElectionGroup
            {
                Id = Guid.Parse(ElectionGroupId),
                Description = "Test Election Group",
                Number = 1,
            },
            BallotGroups = new List<MajorityElectionBallotGroup>
                {
                    new MajorityElectionBallotGroup
                    {
                        Id = Guid.Parse(BallotGroupId),
                        Description = "BG1 long description",
                        ShortDescription = "BG1",
                        Position = 1,
                        Entries = new List<MajorityElectionBallotGroupEntry>
                        {
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupPrimaryElectionEntryId),
                                PrimaryMajorityElectionId = Guid.Parse(ElectionId),
                                BlankRowCount = 0,
                                IndividualCandidatesVoteCount = 1,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateId9InBallotGroup),
                                    },
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        PrimaryElectionCandidateId = Guid.Parse(CandidateId10InBallotGroup),
                                    },
                                },
                            },
                            new MajorityElectionBallotGroupEntry
                            {
                                Id = Guid.Parse(BallotGroupSecondaryElectionEntryId),
                                SecondaryMajorityElectionId = Guid.Parse(SecondaryElectionId),
                                BlankRowCount = 0,
                                Candidates = new List<MajorityElectionBallotGroupEntryCandidate>
                                {
                                    new MajorityElectionBallotGroupEntryCandidate
                                    {
                                        SecondaryElectionCandidateId = Guid.Parse(SecondaryCandidateId4InBallotGroup),
                                    },
                                },
                            },
                        },
                    },
                },
        };
    }

    public static MajorityElectionCandidate BuildCandidate(string candidateId, int position, int checkDigit)
    {
        return new MajorityElectionCandidate
        {
            Id = Guid.Parse(candidateId),
            FirstName = $"{position}firstName",
            LastName = $"{position}lastName",
            PoliticalFirstName = $"{position}pol firstName",
            PoliticalLastName = $"{position}pol last name",
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
            Incumbent = position % 2 == 0,
            Position = position,
            Locality = "locality",
            Number = $"number{position}",
            Sex = (SexType)((position % (Enum.GetValues(typeof(SexType)).Length - 1)) + 1),
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
            CheckDigit = checkDigit,
        };
    }

    public static SecondaryMajorityElectionCandidate BuildSecondaryCandidate(
        string secondaryCandidateId,
        int position,
        int checkDigit,
        string? candidateReferenceId = null)
    {
        return new SecondaryMajorityElectionCandidate
        {
            Id = Guid.Parse(secondaryCandidateId),
            FirstName = $"{position}firstName",
            LastName = $"{position}lastName",
            PoliticalFirstName = $"{position}pol firstName",
            PoliticalLastName = $"{position}pol last name",
            DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc),
            Incumbent = position % 2 == 0,
            Position = position,
            Locality = $"locality{position}",
            Number = $"number{position}",
            Sex = (SexType)((position % (Enum.GetValues(typeof(SexType)).Length - 1)) + 1),
            Title = "title",
            ZipCode = "zip code",
            Origin = "origin",
            CandidateReferenceId = string.IsNullOrEmpty(candidateReferenceId)
                ? null
                : Guid.Parse(candidateReferenceId),
            Translations = TranslationUtil.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                (t, o) => t.Occupation = o,
                "occupation",
                (t, o) => t.OccupationTitle = o,
                "occupation title",
                (t, s) => t.Party = s,
                $"Test{position}"),
            CheckDigit = checkDigit,
        };
    }

    private static void SetResultsMockData(IEnumerable<MajorityElectionResult> results)
    {
        foreach (var result in results)
        {
            SetResultMockData(result);
        }
    }

    private static void SetResultMockData(MajorityElectionResult result)
    {
        result.State = CountingCircleResultState.SubmissionDone;
        result.TotalCountOfVoters = 1000;
        result.ConventionalSubTotal.IndividualVoteCount = 140;
        result.ConventionalSubTotal.EmptyVoteCountExclWriteIns = 10;
        result.ConventionalSubTotal.InvalidVoteCount = 20;
        result.EVotingSubTotal.IndividualVoteCount = 140;
        result.EVotingSubTotal.EmptyVoteCountExclWriteIns = 10;
        result.EVotingSubTotal.EmptyVoteCountWriteIns = 7;
        result.EVotingSubTotal.InvalidVoteCount = 20;

        result.CountOfVoters = new PoliticalBusinessNullableCountOfVoters
        {
            ConventionalReceivedBallots = 500,
            ConventionalInvalidBallots = 200,
            ConventionalBlankBallots = 80,
            ConventionalAccountedBallots = 220,
            VoterParticipation = .5m,
            EVotingReceivedBallots = 1000,
            EVotingInvalidBallots = 400,
            EVotingBlankBallots = 160,
            EVotingAccountedBallots = 440,
        };

        var voteCountByCandidateId = new Dictionary<string, int>
            {
                { CandidateId1, 90 },
                { CandidateId2, 150 },
                { CandidateId3, 100 },
                { CandidateId4, 200 },
                { CandidateId5, 80 },
                { CandidateId6, 70 },
                { CandidateId7, 60 },
                { CandidateId8, 55 },
                { CandidateId9InBallotGroup, 50 },
                { CandidateId10InBallotGroup, 0 },
            };

        // x + 6 for write-ins, x + 4 for non-write-ins
        result.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual = voteCountByCandidateId.Sum(x => (x.Value * 2) + 10);

        var candidateResults = result.CandidateResults.ToDictionary(x => x.CandidateId);
        foreach (var (candidateKey, candidateVoteCount) in voteCountByCandidateId)
        {
            var candidateId = Guid.Parse(candidateKey);
            candidateResults[candidateId].ConventionalVoteCount = candidateVoteCount;
            candidateResults[candidateId].EVotingWriteInsVoteCount = candidateVoteCount + 6;
            candidateResults[candidateId].EVotingExclWriteInsVoteCount = candidateVoteCount + 4;
        }

        var i = 0;
        foreach (var secondaryResult in result.SecondaryMajorityElectionResults)
        {
            SetSecondaryResultMockData(secondaryResult, i++);
        }
    }

    private static void SetSecondaryResultMockData(SecondaryMajorityElectionResult result, int modifier)
    {
        result.ConventionalSubTotal.IndividualVoteCount = 140 + modifier;
        result.ConventionalSubTotal.EmptyVoteCountExclWriteIns = 10 + modifier;
        result.ConventionalSubTotal.InvalidVoteCount = 20 + modifier;
        result.EVotingSubTotal.IndividualVoteCount = 140 + modifier;
        result.EVotingSubTotal.EmptyVoteCountWriteIns = 12 + modifier;
        result.EVotingSubTotal.EmptyVoteCountExclWriteIns = 10 + modifier;
        result.EVotingSubTotal.InvalidVoteCount = 20 + modifier;

        var voteCountByCandidateId = new Dictionary<string, int>
            {
                { SecondaryCandidateId1, 10 },
                { SecondaryCandidateId2, 150 },
                { SecondaryCandidateId3, 150 },
                { SecondaryCandidateId4InBallotGroup, 90 },
                { Secondary2CandidateId1, 80 },
                { Secondary2CandidateId2, 70 },
                { Secondary2CandidateId3, 200 },
                { Secondary2CandidateId4, 55 },
            };

        result.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual = voteCountByCandidateId.Sum(x => x.Value);

        // x + 6 for write-ins, x + 4 for non-write-ins
        result.EVotingSubTotal.TotalCandidateVoteCountExclIndividual = voteCountByCandidateId.Sum(x => (x.Value * 2) + 10);

        var candidateResults = result.CandidateResults.ToDictionary(x => x.CandidateId);
        foreach (var (candidateKey, candidateVoteCount) in voteCountByCandidateId)
        {
            var candidateId = Guid.Parse(candidateKey);

            // all secondary election candidates in dictionary but in the result are only candidates of this secondary election.
            if (candidateResults.TryGetValue(candidateId, out var candidateResult))
            {
                candidateResult.ConventionalVoteCount = candidateVoteCount;
                candidateResult.EVotingWriteInsVoteCount = candidateVoteCount + 6;
                candidateResult.EVotingExclWriteInsVoteCount = candidateVoteCount + 4;
            }
        }
    }
}
