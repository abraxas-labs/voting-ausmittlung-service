// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using BallotNumberGeneration = Abraxas.Voting.Basis.Shared.V1.BallotNumberGeneration;
using BasisEvents = Abraxas.Voting.Basis.Events.V1;
using ContestState = Abraxas.Voting.Basis.Shared.V1.ContestState;
using DomainOfInfluenceCanton = Abraxas.Voting.Basis.Shared.V1.DomainOfInfluenceCanton;
using DomainOfInfluenceType = Abraxas.Voting.Basis.Shared.V1.DomainOfInfluenceType;
using MajorityElectionMandateAlgorithm = Abraxas.Voting.Basis.Shared.V1.MajorityElectionMandateAlgorithm;
using MajorityElectionResultEntry = Abraxas.Voting.Basis.Shared.V1.MajorityElectionResultEntry;
using MajorityElectionReviewProcedure = Abraxas.Voting.Basis.Shared.V1.MajorityElectionReviewProcedure;
using SexType = Abraxas.Voting.Ausmittlung.Shared.V1.SexType;
using SwissAbroadVotingRight = Abraxas.Voting.Basis.Shared.V1.SwissAbroadVotingRight;
using VotingChannel = Abraxas.Voting.Basis.Shared.V1.VotingChannel;

namespace Voting.Ausmittlung.Test.E2ETests;

public class MajorityElectionE2ETest : BaseTest<MajorityElectionResultService.MajorityElectionResultServiceClient>
{
    private const string CountingCircleId = "7d42e80b-aad1-43cb-94a3-11fd725515a5";
    private const string DomainOfInfluenceId = "4b5ab431-384d-4062-8de3-239609e3a733";
    private const string ContestId = "883fbb10-8b7b-4694-b1bf-e84c5657adb7";
    private const string MajorityElectionId = "2b5a668c-1456-4df5-ba4c-7eef8c84a8b3";
    private const string EVotingEch0222File1Mandate = "E2ETests/EVotingFiles/eCH-0222 majority election 1 mandate.xml";
    private const string EVotingEch0222File2Mandates = "E2ETests/EVotingFiles/eCH-0222 majority election 2 mandates.xml";
    private const string EVotingEch0110File1Mandate = "E2ETests/EVotingFiles/eCH-0110 majority election 1 mandate.xml";
    private const string EVotingEch0110File2Mandates = "E2ETests/EVotingFiles/eCH-0110 majority election 2 mandates.xml";

    private const string Candidate1MId1 = "6cb4a328-4c53-448c-8fc0-21a3dd6f0a30";
    private const string Candidate1MId2 = "2e981ee7-ddc6-4fbf-a9c7-23735d75ec04";
    private const string Candidate2MId1 = "6bc1c618-ca30-45c4-b3cb-e6695ac5cafe";
    private const string Candidate2MId2 = "0247dc60-234d-47a0-bc4e-76ddd9164782";
    private const string Candidate2MId3 = "9bf8f444-3c94-473b-8d68-c0381b864a85";
    private const string Candidate2MId4 = "3170cb8a-375a-435e-8cbc-0800b1a13cef";
    private const string Candidate2MId5 = "116947d1-7ef4-4922-90ae-26064966d456";
    private const string Candidate2MId6 = "ec38064d-dc59-4fea-bfe4-54d358e0b4e1";
    private const string Candidate2MId7 = "a8fbab74-d250-4a0e-88b2-2255ead1fe69";
    private const string Candidate2MId8 = "8138bad1-252b-454b-a3b8-008889dfd8b3";

    private static readonly ElectionTestData ElectionTestDataOneMandateTg = new ElectionTestData(
        6171,
        6180,
        2,
        4,
        16,
        5,
        EVotingEch0222File1Mandate,
        EVotingEch0110File1Mandate,
        22,
        4,
        1,
        17,
        null,
        5,
        0,
        new Dictionary<string, (MajorityElectionWriteInMappingTarget Target, string CandidateId)>
        {
            ["Hans Meier"] = (MajorityElectionWriteInMappingTarget.Individual, string.Empty),
            ["Andreas Schelling"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate1MId2),
            ["Hans Fritz"] = (MajorityElectionWriteInMappingTarget.Invalid, string.Empty),
            ["Dummkopf"] = (MajorityElectionWriteInMappingTarget.InvalidBallot, string.Empty),
        },
        new ElectionExpectedResult(
            34,
            39,
            12,
            2,
            1,
            9,
            34,
            6,
            2,
            26,
            0,
            7,
            2),
        new List<CandidateTestData>
        {
            new(Candidate1MId1, 1, 8, new CandidateExpectedResult(3, 11)),
            new(Candidate1MId2, 2, 4, new CandidateExpectedResult(2, 6)),
        });

    private static readonly ElectionTestData ElectionTestDataOneMandateSg = new ElectionTestData(
        6171,
        6180,
        2,
        4,
        16,
        5,
        EVotingEch0222File1Mandate,
        EVotingEch0110File1Mandate,
        22,
        4,
        1,
        17,
        null,
        5,
        0,
        new Dictionary<string, (MajorityElectionWriteInMappingTarget Target, string CandidateId)>
        {
            ["Hans Meier"] = (MajorityElectionWriteInMappingTarget.Individual, string.Empty),
            ["Andreas Schelling"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate1MId2),
            ["Hans Fritz"] = (MajorityElectionWriteInMappingTarget.Empty, string.Empty),
            ["Dummkopf"] = (MajorityElectionWriteInMappingTarget.InvalidBallot, string.Empty),
        },
        new ElectionExpectedResult(
            34,
            39,
            12,
            2,
            3,
            7,
            34,
            6,
            4,
            24,
            0,
            7,
            0),
        new List<CandidateTestData>
        {
            new(Candidate1MId1, 1, 8, new CandidateExpectedResult(3, 11)),
            new(Candidate1MId2, 2, 4, new CandidateExpectedResult(2, 6)),
        });

    private static readonly ElectionTestData ElectionTestDataTwoMandatesSg = new ElectionTestData(
        6171,
        6180,
        2,
        4,
        16,
        5,
        EVotingEch0222File2Mandates,
        EVotingEch0110File2Mandates,
        22,
        4,
        1,
        17,
        1,
        5,
        0,
        new Dictionary<string, (MajorityElectionWriteInMappingTarget Target, string CandidateId)>
        {
            ["Hans Meier"] = (MajorityElectionWriteInMappingTarget.Individual, string.Empty),
            ["Dieter Meier"] = (MajorityElectionWriteInMappingTarget.Individual, string.Empty),
            ["Hans Muster"] = (MajorityElectionWriteInMappingTarget.Individual, string.Empty),
            ["Esther Freidli"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate2MId1),
            ["Esther Friedli"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate2MId1),
            ["Friedli Ester"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate2MId1),
            ["Beni Wurth"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate2MId2),
            ["Hans Fritz"] = (MajorityElectionWriteInMappingTarget.Empty, string.Empty),
            ["Dummkopf"] = (MajorityElectionWriteInMappingTarget.InvalidBallot, string.Empty),
        },
        new ElectionExpectedResult(
            45,
            50,
            23,
            3,
            2,
            18,
            45,
            7,
            3,
            35,
            6,
            9,
            0),
        new List<CandidateTestData>
        {
            new(Candidate2MId1, 1, 19, new CandidateExpectedResult(15, 34)),
            new(Candidate2MId2, 2, 7, new CandidateExpectedResult(9, 16)),
            new(Candidate2MId3, 3, 2, new CandidateExpectedResult(3, 5)),
            new(Candidate2MId4, 4, 0, new CandidateExpectedResult(0, 0)),
            new(Candidate2MId5, 5, 0, new CandidateExpectedResult(0, 0)),
            new(Candidate2MId6, 6, 0, new CandidateExpectedResult(0, 0)),
            new(Candidate2MId7, 7, 0, new CandidateExpectedResult(0, 0)),
            new(Candidate2MId8, 8, 0, new CandidateExpectedResult(0, 0)),
        });

    private static readonly ElectionTestData ElectionTestDataTwoMandatesTg = new ElectionTestData(
        6171,
        6180,
        2,
        4,
        16,
        5,
        EVotingEch0222File2Mandates,
        EVotingEch0110File2Mandates,
        22,
        4,
        1,
        17,
        1,
        5,
        0,
        new Dictionary<string, (MajorityElectionWriteInMappingTarget Target, string CandidateId)>
        {
            ["Hans Meier"] = (MajorityElectionWriteInMappingTarget.Individual, string.Empty),
            ["Dieter Meier"] = (MajorityElectionWriteInMappingTarget.Individual, string.Empty),
            ["Hans Muster"] = (MajorityElectionWriteInMappingTarget.Individual, string.Empty),
            ["Esther Freidli"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate2MId1),
            ["Esther Friedli"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate2MId1),
            ["Friedli Ester"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate2MId1),
            ["Beni Wurth"] = (MajorityElectionWriteInMappingTarget.Candidate, Candidate2MId2),
            ["Hans Fritz"] = (MajorityElectionWriteInMappingTarget.Invalid, string.Empty),
            ["Dummkopf"] = (MajorityElectionWriteInMappingTarget.InvalidBallot, string.Empty),
        },
        new ElectionExpectedResult(
            45,
            50,
            23,
            3,
            1,
            19,
            45,
            7,
            2,
            36,
            4,
            9,
            4),
        new List<CandidateTestData>
        {
            new(Candidate2MId1, 1, 19, new CandidateExpectedResult(15, 34)),
            new(Candidate2MId2, 2, 7, new CandidateExpectedResult(9, 16)),
            new(Candidate2MId3, 3, 2, new CandidateExpectedResult(3, 5)),
            new(Candidate2MId4, 4, 0, new CandidateExpectedResult(0, 0)),
            new(Candidate2MId5, 5, 0, new CandidateExpectedResult(0, 0)),
            new(Candidate2MId6, 6, 0, new CandidateExpectedResult(0, 0)),
            new(Candidate2MId7, 7, 0, new CandidateExpectedResult(0, 0)),
            new(Candidate2MId8, 8, 0, new CandidateExpectedResult(0, 0)),
        });

    private static readonly string ElectionResultId = AusmittlungUuidV5
        .BuildPoliticalBusinessResult(Guid.Parse(MajorityElectionId), Guid.Parse(CountingCircleId), true).ToString();

    private long _mockedBasisEventInfoSeconds = 1594979476;

    public MajorityElectionE2ETest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task MajorityElectionOneMandateSgE2E()
    {
        var endResult = await RunMajorityElectionEndToEnd(DomainOfInfluenceCanton.Sg, 1, ElectionTestDataOneMandateSg);
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task MajorityElectionOneMandateTgE2E()
    {
        var endResult = await RunMajorityElectionEndToEnd(DomainOfInfluenceCanton.Tg, 1, ElectionTestDataOneMandateTg);
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task MajorityElectionTwoMandatesSgE2E()
    {
        var endResult = await RunMajorityElectionEndToEnd(DomainOfInfluenceCanton.Sg, 2, ElectionTestDataTwoMandatesSg);
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task MajorityElectionTwoMandatesTgE2E()
    {
        var endResult = await RunMajorityElectionEndToEnd(DomainOfInfluenceCanton.Tg, 2, ElectionTestDataTwoMandatesTg);
        endResult.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        // Not relevant for this class, but we need to provide one anyway
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .GetEndResultAsync(new GetMajorityElectionEndResultRequest
            {
                MajorityElectionId = MajorityElectionId,
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        // Skip this test, not needed here.
        yield break;
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        // Skip this test, not needed here.
        yield break;
    }

    private async Task<MajorityElectionEndResult> RunMajorityElectionEndToEnd(DomainOfInfluenceCanton canton, int numberOfMandates, ElectionTestData testData)
    {
        await SetupContestAndMajorityElection(canton, numberOfMandates, testData);
        await EnterContestDetails(testData);
        await ImportEVotingResults(testData);
        await EnterResults(testData);

        var monitoringResultService = CreateService<MajorityElectionResultService.MajorityElectionResultServiceClient>(
            RolesMockedData.MonitoringElectionAdmin);
        var endResult = await monitoringResultService.GetEndResultAsync(new GetMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionId,
        });

        // Check voting cards ("Stimmrechtsausweise")
        var paperVotingCards = endResult.VotingCards.First(vc =>
            vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.Paper);
        paperVotingCards.CountOfReceivedVotingCards.Should().Be(testData.VotingCardsPaper);
        var ballotVotingCards = endResult.VotingCards.First(vc =>
            vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.BallotBox);
        ballotVotingCards.CountOfReceivedVotingCards.Should().Be(testData.VotingCardsBallotBox);
        var validMailVotingCards = endResult.VotingCards.First(vc =>
            vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail);
        validMailVotingCards.CountOfReceivedVotingCards.Should().Be(testData.VotingCardsByMailValid);
        var invalidMailVotingCards = endResult.VotingCards.First(vc =>
            !vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail);
        invalidMailVotingCards.CountOfReceivedVotingCards.Should().Be(testData.VotingCardsByMailInvalid);

        var validVotingCards = endResult.VotingCards
            .Where(vc => vc.Valid)
            .Sum(vc => vc.CountOfReceivedVotingCards);
        validVotingCards.Should().Be(testData.ExpectedResult.VotingCardsValidTotal);
        var countOfVotingCards = endResult.VotingCards.Sum(vc => vc.CountOfReceivedVotingCards);
        countOfVotingCards.Should().Be(testData.ExpectedResult.VotingCardsTotal);

        // Check ballots ("Wahlzettel")
        endResult.CountOfVoters.ConventionalReceivedBallots.Should().Be(testData.ConventionalReceivedBallots);
        endResult.CountOfVoters.ConventionalBlankBallots.Should().Be(testData.ConventionalBlankBallots);
        endResult.CountOfVoters.ConventionalInvalidBallots.Should().Be(testData.ConventionalInvalidBallots);
        endResult.CountOfVoters.ConventionalAccountedBallots.Should().Be(testData.ConventionalAccountedBallots);

        endResult.CountOfVoters.EVotingReceivedBallots.Should().Be(testData.ExpectedResult.EVotingReceivedBallots);
        endResult.CountOfVoters.EVotingBlankBallots.Should().Be(testData.ExpectedResult.EVotingBlankBallots);
        endResult.CountOfVoters.EVotingInvalidBallots.Should().Be(testData.ExpectedResult.EVotingInvalidBallots);
        endResult.CountOfVoters.EVotingAccountedBallots.Should().Be(testData.ExpectedResult.EVotingAccountedBallots);

        endResult.CountOfVoters.TotalReceivedBallots.Should().Be(testData.ExpectedResult.TotalReceivedBallots);
        endResult.CountOfVoters.TotalBlankBallots.Should().Be(testData.ExpectedResult.TotalBlankBallots);
        endResult.CountOfVoters.TotalInvalidBallots.Should().Be(testData.ExpectedResult.TotalInvalidBallots);
        endResult.CountOfVoters.TotalAccountedBallots.Should().Be(testData.ExpectedResult.TotalAccountedBallots);

        endResult.EmptyVoteCount.Should().Be(testData.ExpectedResult.EmptyVoteCount);
        endResult.InvalidVoteCount.Should().Be(testData.ExpectedResult.InvalidVoteCount);
        endResult.IndividualVoteCount.Should().Be(testData.ExpectedResult.IndividualVoteCount);

        foreach (var candidateResult in endResult.CandidateEndResults)
        {
            var expectedCandidateResult = testData.Candidates.First(c => c.Id == candidateResult.Candidate.Id).ExpectedResult;
            candidateResult.EVotingVoteCount.Should().Be(expectedCandidateResult.EVotingVoteCount);
            candidateResult.VoteCount.Should().Be(expectedCandidateResult.VoteCount);
        }

        return endResult;
    }

    private async Task SetupContestAndMajorityElection(DomainOfInfluenceCanton canton, int numberOfMandates, ElectionTestData electionTestData)
    {
        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.CantonSettingsCreated
        {
            CantonSettings = new CantonSettingsEventData
            {
                Id = Guid.NewGuid().ToString(),
                Canton = canton,
                AuthorityName = $"KT {canton.ToString().ToUpperInvariant()}",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                EnabledVotingCardChannels =
                {
                    new CantonSettingsVotingCardChannelEventData
                    {
                        Valid = true,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new CantonSettingsVotingCardChannelEventData
                    {
                        Valid = false,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new CantonSettingsVotingCardChannelEventData
                    {
                        Valid = true,
                        VotingChannel = VotingChannel.BallotBox,
                    },
                    new CantonSettingsVotingCardChannelEventData
                    {
                        Valid = true,
                        VotingChannel = VotingChannel.Paper,
                    },
                },
                MajorityElectionInvalidVotes = canton == DomainOfInfluenceCanton.Tg,
                MajorityElectionAbsoluteMajorityAlgorithm = canton == DomainOfInfluenceCanton.Tg
                    ? CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates
                    : CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo,
                SwissAbroadVotingRight = SwissAbroadVotingRight.SeparateCountingCircle,
                MajorityElectionUseCandidateCheckDigit = true,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.CountingCircleCreated
        {
            CountingCircle = new CountingCircleEventData
            {
                Id = CountingCircleId,
                Bfs = "123",
                Code = "123",
                Name = "Testgemeinde",
                SortNumber = 1,
                ResponsibleAuthority = new AuthorityEventData
                {
                    Name = "Testgemeinde",
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                },
                ContactPersonDuringEvent = new ContactPersonEventData
                {
                    Email = "test@example.com",
                    FirstName = "Toni",
                    FamilyName = "Tester",
                    Phone = "+41799999999",
                    MobilePhone = "41799999988",
                },
                ContactPersonSameDuringEventAsAfter = true,
                EVoting = true,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.DomainOfInfluenceCreated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = DomainOfInfluenceId,
                Bfs = "321",
                Code = "321",
                Name = "Test-Oberbehörde",
                ShortName = "Test",
                Canton = canton,
                SortNumber = 1,
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                AuthorityName = SecureConnectTestDefaults.MockedTenantDefault.Name,
                Type = DomainOfInfluenceType.Ch,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.DomainOfInfluenceCountingCircleEntriesUpdated
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceId,
                CountingCircleIds = { CountingCircleId },
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ContestCreated
        {
            Contest = new ContestEventData
            {
                Id = ContestId,
                DomainOfInfluenceId = DomainOfInfluenceId,
                Date = MockedClock.GetDate(2).ToTimestamp(),
                EndOfTestingPhase = MockedClock.GetDate(-1).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("contest desc") },
                State = ContestState.TestingPhase,
                EVoting = true,
                EVotingFrom = MockedClock.GetDate().ToTimestamp(),
                EVotingTo = MockedClock.GetDate(1).ToTimestamp(),
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.MajorityElectionCreated
        {
            MajorityElection = new MajorityElectionEventData
            {
                Id = MajorityElectionId,
                ContestId = ContestId,
                DomainOfInfluenceId = DomainOfInfluenceId,
                InternalDescription = "internal desc",
                OfficialDescription = { LanguageUtil.MockAllLanguages("official desc") },
                ShortDescription = { LanguageUtil.MockAllLanguages("short desc") },
                MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
                NumberOfMandates = numberOfMandates,
                PoliticalBusinessNumber = "01",
                AutomaticEmptyVoteCounting = true,
                InvalidVotes = canton == DomainOfInfluenceCanton.Tg,
                ResultEntry = MajorityElectionResultEntry.FinalResults,
                ReviewProcedure = MajorityElectionReviewProcedure.Physically,
                BallotBundleSize = 25,
                BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
                BallotBundleSampleSize = 2,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        var candidateNumber = 1;
        foreach (var candidate in electionTestData.Candidates)
        {
            await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.MajorityElectionCandidateCreated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = candidate.Id,
                    MajorityElectionId = MajorityElectionId,
                    Position = candidate.Position,
                    Number = candidateNumber.ToString("00"),
                    DateOfBirth = MockedClock.GetDate(-7000).ToTimestamp(),
                    Sex = Abraxas.Voting.Basis.Shared.V1.SexType.Male,
                    FirstName = "first" + candidateNumber,
                    LastName = "last" + candidateNumber,
                    PoliticalFirstName = "first" + candidateNumber,
                    PoliticalLastName = "last" + candidateNumber,
                    Party = { LanguageUtil.MockAllLanguages("party") },
                    Locality = "home",
                },
                EventInfo = GetMockedBasisEventInfo(),
            });

            candidateNumber++;
        }

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.MajorityElectionActiveStateUpdated
        {
            MajorityElectionId = MajorityElectionId,
            Active = true,
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ContestTestingPhaseEnded
        {
            ContestId = ContestId,
            EventInfo = GetMockedBasisEventInfo(),
        });
    }

    private async Task EnterContestDetails(ElectionTestData testData)
    {
        EventPublisherMock.Clear();

        var contestDetailsService = CreateService<ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceClient>(
            RolesMockedData.ErfassungElectionAdmin);
        await contestDetailsService.UpdateDetailsAsync(new UpdateContestCountingCircleDetailsRequest
        {
            ContestId = ContestId,
            CountingCircleId = CountingCircleId,
            VotingCards =
            {
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = true,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.Paper,
                    CountOfReceivedVotingCards = testData.VotingCardsPaper,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = true,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.BallotBox,
                    CountOfReceivedVotingCards = testData.VotingCardsBallotBox,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = true,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail,
                    CountOfReceivedVotingCards = testData.VotingCardsByMailValid,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = false,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail,
                    CountOfReceivedVotingCards = testData.VotingCardsByMailInvalid,
                },
            },
            CountOfVoters =
            {
                new UpdateCountOfVotersInformationSubTotalRequest
                {
                    CountOfVoters = testData.CountOfVotersMale,
                    Sex = SexType.Male,
                    VoterType = VoterType.Swiss,
                },
                new UpdateCountOfVotersInformationSubTotalRequest
                {
                    CountOfVoters = testData.CountOfVotersFemale,
                    Sex = SexType.Female,
                    VoterType = VoterType.Swiss,
                },
            },
        });

        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ContestCountingCircleDetailsCreated>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private async Task ImportEVotingResults(ElectionTestData electionTestData)
    {
        var uri = new Uri($"api/result_import/{ContestId}", UriKind.RelativeOrAbsolute);
        using var httpClient = CreateHttpClient(RolesMockedData.MonitoringElectionAdmin);
        using var resp = await httpClient.PostFiles(
            uri,
            ("ech0222File", electionTestData.Ech0222File),
            ("ech0110File", electionTestData.Ech0110File));
        resp.EnsureSuccessStatusCode();

        EventPublisherMock.AllPublishedEvents.Should()
            .HaveCountGreaterOrEqualTo(11)
            .And.HaveCountLessOrEqualTo(15);
        EventPublisherMock.GetPublishedEvents<ResultImportCreated>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ResultImportStarted>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<CountingCircleVotingCardsImported>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultImported>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<MajorityElectionWriteInBallotImported>().Should()
            .HaveCountGreaterOrEqualTo(6)
            .And.HaveCountLessOrEqualTo(10);
        EventPublisherMock.GetPublishedEvents<ResultImportCompleted>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private async Task EnterResults(ElectionTestData electionTestData)
    {
        // This starts the result submission
        var resultService = CreateService<ResultService.ResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
        await resultService.GetListAsync(new GetResultListRequest
        {
            ContestId = ContestId,
            CountingCircleId = CountingCircleId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultSubmissionStarted>().Should().HaveCount(1);
        await RunAllEvents();

        var electionResultService = CreateService<MajorityElectionResultService.MajorityElectionResultServiceClient>(
            RolesMockedData.ErfassungElectionAdmin);

        await electionResultService.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = ElectionResultId,
            ResultEntry = Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionResultEntry.FinalResults,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultEntryDefined>().Should().HaveCount(1);
        await RunAllEvents();

        await electionResultService.EnterCandidateResultsAsync(new EnterMajorityElectionCandidateResultsRequest
        {
            ElectionResultId = ElectionResultId,
            CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalReceivedBallots = electionTestData.ConventionalReceivedBallots,
                ConventionalBlankBallots = electionTestData.ConventionalBlankBallots,
                ConventionalInvalidBallots = electionTestData.ConventionalInvalidBallots,
                ConventionalAccountedBallots = electionTestData.ConventionalAccountedBallots,
            },
            EmptyVoteCount = electionTestData.EmptyVoteCount,
            IndividualVoteCount = electionTestData.IndividualVoteCount,
            InvalidVoteCount = electionTestData.InvalidVoteCount,
            CandidateResults =
            {
                electionTestData.Candidates.Select(c => new EnterMajorityElectionCandidateResultRequest
                {
                    CandidateId = c.Id,
                    VoteCount = c.ConventionalVoteCount,
                }),
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultCountOfVotersEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<MajorityElectionCandidateResultsEntered>().Should().HaveCount(1);
        await RunAllEvents();

        var resultImportService = CreateService<ResultImportService.ResultImportServiceClient>(RolesMockedData.ErfassungElectionAdmin);
        var mappings = await resultImportService.GetMajorityElectionWriteInMappingsAsync(new GetMajorityElectionWriteInMappingsRequest
        {
            ContestId = ContestId,
            CountingCircleId = CountingCircleId,
        });
        var electionMapping = mappings.ElectionWriteInMappings[0];
        await resultImportService.MapMajorityElectionWriteInsAsync(new MapMajorityElectionWriteInsRequest
        {
            ImportId = mappings.ImportId,
            ElectionId = electionMapping.Election.Id,
            CountingCircleId = CountingCircleId,
            PoliticalBusinessType = electionMapping.Election.BusinessType,
            Mappings =
            {
                electionMapping.WriteInMappings.Select(m => new MapMajorityElectionWriteInRequest
                {
                    WriteInId = m.Id,
                    Target = electionTestData.WriteInMappings[m.WriteInCandidateName].Target,
                    CandidateId = electionTestData.WriteInMappings[m.WriteInCandidateName].CandidateId,
                }),
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<MajorityElectionWriteInsMapped>().Should().HaveCount(1);
        await RunAllEvents();

        await electionResultService.SubmissionFinishedAsync(new MajorityElectionResultSubmissionFinishedRequest
        {
            ElectionResultId = ElectionResultId,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultSubmissionFinished>().Should().HaveCount(1);
        await RunAllEvents();

        var monitoringResultService = CreateService<MajorityElectionResultService.MajorityElectionResultServiceClient>(
            RolesMockedData.MonitoringElectionAdmin);
        await monitoringResultService.AuditedTentativelyAsync(new MajorityElectionResultAuditedTentativelyRequest
        {
            ElectionResultIds = { ElectionResultId },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultAuditedTentatively>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultPublished>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private EventInfo GetMockedBasisEventInfo()
    {
        return new EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = _mockedBasisEventInfoSeconds++,
            },
            Tenant = new EventInfoTenant
            {
                Id = SecureConnectTestDefaults.MockedTenantDefault.Id,
                Name = SecureConnectTestDefaults.MockedTenantDefault.Name,
            },
            User = new EventInfoUser
            {
                Id = SecureConnectTestDefaults.MockedUserDefault.Loginid,
                FirstName = SecureConnectTestDefaults.MockedUserDefault.Firstname,
                LastName = SecureConnectTestDefaults.MockedUserDefault.Lastname,
                Username = SecureConnectTestDefaults.MockedUserDefault.Username,
            },
        };
    }

    private record ElectionTestData(
        int CountOfVotersMale,
        int CountOfVotersFemale,
        int VotingCardsPaper,
        int VotingCardsBallotBox,
        int VotingCardsByMailValid,
        int VotingCardsByMailInvalid,
        string Ech0222File,
        string Ech0110File,
        int ConventionalReceivedBallots,
        int ConventionalBlankBallots,
        int ConventionalInvalidBallots,
        int ConventionalAccountedBallots,
        int? EmptyVoteCount,
        int IndividualVoteCount,
        int InvalidVoteCount,
        Dictionary<string, (MajorityElectionWriteInMappingTarget Target, string CandidateId)> WriteInMappings,
        ElectionExpectedResult ExpectedResult,
        List<CandidateTestData> Candidates);

    private record ElectionExpectedResult(
        int VotingCardsValidTotal,
        int VotingCardsTotal,
        int EVotingReceivedBallots,
        int EVotingBlankBallots,
        int EVotingInvalidBallots,
        int EVotingAccountedBallots,
        int TotalReceivedBallots,
        int TotalBlankBallots,
        int TotalInvalidBallots,
        int TotalAccountedBallots,
        int EmptyVoteCount,
        int IndividualVoteCount,
        int InvalidVoteCount);

    private record CandidateTestData(string Id, int Position, int ConventionalVoteCount, CandidateExpectedResult ExpectedResult);

    private record CandidateExpectedResult(int EVotingVoteCount, int VoteCount);
}
