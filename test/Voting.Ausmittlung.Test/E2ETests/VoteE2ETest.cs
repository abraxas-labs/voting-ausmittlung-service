// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
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
using BasisEvents = Abraxas.Voting.Basis.Events.V1;
using ContestCountingCircleDetailsCreated = Abraxas.Voting.Ausmittlung.Events.V2.ContestCountingCircleDetailsCreated;
using DomainOfInfluenceType = Abraxas.Voting.Basis.Shared.V1.DomainOfInfluenceType;
using SexType = Abraxas.Voting.Ausmittlung.Shared.V1.SexType;
using VoteResultEntry = Abraxas.Voting.Basis.Shared.V1.VoteResultEntry;
using VoteReviewProcedure = Abraxas.Voting.Basis.Shared.V1.VoteReviewProcedure;
using VotingChannel = Abraxas.Voting.Basis.Shared.V1.VotingChannel;

namespace Voting.Ausmittlung.Test.E2ETests;

public class VoteE2ETest : BaseTest<VoteResultService.VoteResultServiceClient>
{
    private const string CountingCircleId = "b926bd58-561b-4a10-8f23-2bd8c7c049d4";
    private const string DomainOfInfluenceId = "651ea5fa-dd87-4bcb-bbc3-d5a9ccde8151";
    private const string ContestId = "904e4192-2a82-41b2-969e-c64310c4631f";
    private const string EVotingEch0222File = "E2ETests/EVotingFiles/eCH-0222 vote.xml";
    private const string EVotingEch0110File = "E2ETests/EVotingFiles/eCH-0110 vote.xml";

    private const string Vote1Id = "90db57b6-5947-4623-b1d6-f5323f1ff881";
    private const string Vote2Id = "c9963428-985d-4668-8704-15c3067ab675";
    private const string Vote3Id = "6cec9d52-7b2f-4c9e-b009-02812d537d57";

    private const string Vote1BallotId = "ca7e0537-a822-4460-89c2-9937f5510da9";
    private const string Vote2BallotId = "94be93ce-bd0e-4c28-9ed4-63c3f6e33578";
    private const string Vote3BallotId = "4e738791-66f2-448c-a023-e6d2cc6db2b4";

    private static readonly List<VoteResultExpectedData> ExpectedData = new()
    {
        new VoteResultExpectedData(
            Vote1Id,
            new ExpectedVotingCards(2, 4, 19, 16, 5, 41, 46),
            new ExpectedCountOfVoters(22, 3, 1, 18),
            new ExpectedCountOfVoters(19, 3, 0, 16),
            new ExpectedCountOfVoters(41, 6, 1, 34),
            new List<QuestionResult>
            {
                new QuestionResult(
                    new QuestionYesNoResult(15, 2, 1),
                    new QuestionYesNoResult(7, 6, 3),
                    new QuestionYesNoResult(22, 8, 4),
                    true),
                new QuestionResult(
                    new QuestionYesNoResult(5, 10, 3),
                    new QuestionYesNoResult(5, 10, 1),
                    new QuestionYesNoResult(10, 20, 4),
                    false),
            },
            new List<TieBreakQuestionResult>
            {
                new TieBreakQuestionResult(
                    new QuestionQ1Q2Result(12, 5, 1),
                    new QuestionQ1Q2Result(8, 4, 4),
                    new QuestionQ1Q2Result(20, 9, 5),
                    true),
            }),
        new VoteResultExpectedData(
            Vote2Id,
            new ExpectedVotingCards(2, 4, 19, 16, 5, 41, 46),
            new ExpectedCountOfVoters(21, 1, 2, 18),
            new ExpectedCountOfVoters(19, 5, 0, 14),
            new ExpectedCountOfVoters(40, 6, 2, 32),
            new List<QuestionResult>
            {
                new QuestionResult(
                    new QuestionYesNoResult(10, 8, 0),
                    new QuestionYesNoResult(10, 4, 0),
                    new QuestionYesNoResult(20, 12, 0),
                    true),
            },
            new List<TieBreakQuestionResult>()),
        new VoteResultExpectedData(
            Vote3Id,
            new ExpectedVotingCards(2, 4, 19, 16, 5, 41, 46),
            new ExpectedCountOfVoters(19, 0, 4, 15),
            new ExpectedCountOfVoters(19, 4, 0, 15),
            new ExpectedCountOfVoters(38, 4, 4, 30),
            new List<QuestionResult>
            {
                new QuestionResult(
                    new QuestionYesNoResult(2, 13, 0),
                    new QuestionYesNoResult(7, 8, 0),
                    new QuestionYesNoResult(9, 21, 0),
                    false),
            },
            new List<TieBreakQuestionResult>()),
    };

    private static readonly string Vote1ResultId = AusmittlungUuidV5
        .BuildPoliticalBusinessResult(Guid.Parse(Vote1Id), Guid.Parse(CountingCircleId), true).ToString();

    private static readonly string Vote2ResultId = AusmittlungUuidV5
        .BuildPoliticalBusinessResult(Guid.Parse(Vote2Id), Guid.Parse(CountingCircleId), true).ToString();

    private static readonly string Vote3ResultId = AusmittlungUuidV5
        .BuildPoliticalBusinessResult(Guid.Parse(Vote3Id), Guid.Parse(CountingCircleId), true).ToString();

    private static readonly string[] VoteResultIds = { Vote1ResultId, Vote2ResultId, Vote3ResultId };

    private long _mockedBasisEventInfoSeconds = 1594979476;

    public VoteE2ETest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task VoteShouldWorkEndToEnd()
    {
        await SetupContestAndVotes();
        await SecondFactorTransactionMockedData.Seed(RunScoped);
        await EnterContestDetails();
        await ImportEVotingResults();
        await EnterResults();

        var monitoringResultService = CreateService<VoteResultService.VoteResultServiceClient>(RolesMockedData.MonitoringElectionAdmin);
        var i = 1;

        foreach (var expectedData in ExpectedData)
        {
            var endResult = await monitoringResultService.GetEndResultAsync(new GetVoteEndResultRequest
            {
                VoteId = expectedData.VoteId,
            });

            // Check voting cards ("Stimmrechtsausweise")
            var paperVotingCards = endResult.VotingCards.First(vc =>
                vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.Paper);
            paperVotingCards.CountOfReceivedVotingCards.Should().Be(expectedData.VotingCards.Paper);
            var ballotVotingCards = endResult.VotingCards.First(vc =>
                vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.BallotBox);
            ballotVotingCards.CountOfReceivedVotingCards.Should().Be(expectedData.VotingCards.BallotBox);
            var eVotingVotingCards = endResult.VotingCards.First(vc =>
                vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.EVoting);
            eVotingVotingCards.CountOfReceivedVotingCards.Should().Be(expectedData.VotingCards.EVoting);
            var validMailVotingCards = endResult.VotingCards.First(vc =>
                vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail);
            validMailVotingCards.CountOfReceivedVotingCards.Should().Be(expectedData.VotingCards.MailValid);
            var invalidMailVotingCards = endResult.VotingCards.First(vc =>
                !vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail);
            invalidMailVotingCards.CountOfReceivedVotingCards.Should().Be(expectedData.VotingCards.MailInvalid);
            var validVotingCards = endResult.VotingCards.Where(vc => vc.Valid).Sum(vc => vc.CountOfReceivedVotingCards);
            validVotingCards.Should().Be(expectedData.VotingCards.TotalValid);
            var countOfVotingCards = endResult.VotingCards.Sum(vc => vc.CountOfReceivedVotingCards);
            countOfVotingCards.Should().Be(expectedData.VotingCards.TotalReceived);

            // Check ballots ("Wahlzettel")
            endResult.BallotEndResults.Should().HaveCount(1);
            var ballotEndResult = endResult.BallotEndResults[0];
            ballotEndResult.CountOfVoters.ConventionalSubTotal.ReceivedBallots.Should().Be(expectedData.ConventionalCountOfVoters.ReceivedBallots);
            ballotEndResult.CountOfVoters.ConventionalSubTotal.BlankBallots.Should().Be(expectedData.ConventionalCountOfVoters.BlankBallots);
            ballotEndResult.CountOfVoters.ConventionalSubTotal.InvalidBallots.Should().Be(expectedData.ConventionalCountOfVoters.InvalidBallots);
            ballotEndResult.CountOfVoters.ConventionalSubTotal.AccountedBallots.Should().Be(expectedData.ConventionalCountOfVoters.AccountedBallots);

            ballotEndResult.CountOfVoters.EVotingSubTotal.ReceivedBallots.Should().Be(expectedData.EVotingCountOfVoters.ReceivedBallots);
            ballotEndResult.CountOfVoters.EVotingSubTotal.BlankBallots.Should().Be(expectedData.EVotingCountOfVoters.BlankBallots);
            ballotEndResult.CountOfVoters.EVotingSubTotal.InvalidBallots.Should().Be(expectedData.EVotingCountOfVoters.InvalidBallots);
            ballotEndResult.CountOfVoters.EVotingSubTotal.AccountedBallots.Should().Be(expectedData.EVotingCountOfVoters.AccountedBallots);

            ballotEndResult.CountOfVoters.TotalReceivedBallots.Should().Be(expectedData.TotalCountOfVoters.ReceivedBallots);
            ballotEndResult.CountOfVoters.TotalBlankBallots.Should().Be(expectedData.TotalCountOfVoters.BlankBallots);
            ballotEndResult.CountOfVoters.TotalInvalidBallots.Should().Be(expectedData.TotalCountOfVoters.InvalidBallots);
            ballotEndResult.CountOfVoters.TotalAccountedBallots.Should().Be(expectedData.TotalCountOfVoters.AccountedBallots);

            ballotEndResult.QuestionEndResults.Count.Should().Be(expectedData.QuestionResults.Count);
            for (var j = 0; j < ballotEndResult.QuestionEndResults.Count; j++)
            {
                var questionResult = ballotEndResult.QuestionEndResults[j];
                var expectedResult = expectedData.QuestionResults[j];
                questionResult.ConventionalSubTotal.TotalCountOfAnswerYes.Should().Be(expectedResult.ConventionalCount.Yes);
                questionResult.ConventionalSubTotal.TotalCountOfAnswerNo.Should().Be(expectedResult.ConventionalCount.No);
                questionResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified.Should().Be(expectedResult.ConventionalCount.Unspecified);
                questionResult.EVotingSubTotal.TotalCountOfAnswerYes.Should().Be(expectedResult.EVotingCount.Yes);
                questionResult.EVotingSubTotal.TotalCountOfAnswerNo.Should().Be(expectedResult.EVotingCount.No);
                questionResult.EVotingSubTotal.TotalCountOfAnswerUnspecified.Should().Be(expectedResult.EVotingCount.Unspecified);
                questionResult.TotalCountOfAnswerYes.Should().Be(expectedResult.TotalCount.Yes);
                questionResult.TotalCountOfAnswerNo.Should().Be(expectedResult.TotalCount.No);
                questionResult.TotalCountOfAnswerUnspecified.Should().Be(expectedResult.TotalCount.Unspecified);
                questionResult.Accepted.Should().Be(expectedResult.Accepted);
            }

            ballotEndResult.TieBreakQuestionEndResults.Count.Should().Be(expectedData.TieBreakQuestionResults.Count);
            for (var j = 0; j < ballotEndResult.TieBreakQuestionEndResults.Count; j++)
            {
                var questionResult = ballotEndResult.TieBreakQuestionEndResults[j];
                var expectedResult = expectedData.TieBreakQuestionResults[j];
                questionResult.ConventionalSubTotal.TotalCountOfAnswerQ1.Should().Be(expectedResult.ConventionalCount.Q1);
                questionResult.ConventionalSubTotal.TotalCountOfAnswerQ2.Should().Be(expectedResult.ConventionalCount.Q2);
                questionResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified.Should().Be(expectedResult.ConventionalCount.Unspecified);
                questionResult.EVotingSubTotal.TotalCountOfAnswerQ1.Should().Be(expectedResult.EVotingCount.Q1);
                questionResult.EVotingSubTotal.TotalCountOfAnswerQ2.Should().Be(expectedResult.EVotingCount.Q2);
                questionResult.EVotingSubTotal.TotalCountOfAnswerUnspecified.Should().Be(expectedResult.EVotingCount.Unspecified);
                questionResult.TotalCountOfAnswerQ1.Should().Be(expectedResult.TotalCount.Q1);
                questionResult.TotalCountOfAnswerQ2.Should().Be(expectedResult.TotalCount.Q2);
                questionResult.TotalCountOfAnswerUnspecified.Should().Be(expectedResult.TotalCount.Unspecified);
                questionResult.Q1Accepted.Should().Be(expectedResult.Q1Accepted);
            }

            endResult.MatchSnapshot(i.ToString());
            i++;
        }
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        // Not relevant for this class, but we need to provide one anyway
        await new VoteResultService.VoteResultServiceClient(channel)
            .GetEndResultAsync(new GetVoteEndResultRequest
            {
                VoteId = Vote1Id,
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

    private async Task SetupContestAndVotes()
    {
        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.CantonSettingsCreated
        {
            CantonSettings = new CantonSettingsEventData
            {
                Id = Guid.NewGuid().ToString(),
                Canton = DomainOfInfluenceCanton.Sg,
                AuthorityName = "KT SG",
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
                SwissAbroadVotingRight = SwissAbroadVotingRight.SeparateCountingCircle,
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
                Name = "Auslandschweizer Kanton St.Gallen",
                SortNumber = 1,
                ResponsibleAuthority = new AuthorityEventData
                {
                    Name = "Kanton SG",
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
                Name = "Kanton St. Gallen",
                ShortName = "KT SG",
                Canton = DomainOfInfluenceCanton.Sg,
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

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.VoteCreated
        {
            Vote = new VoteEventData
            {
                Id = Vote1Id,
                ContestId = ContestId,
                DomainOfInfluenceId = DomainOfInfluenceId,
                InternalDescription = "Einheitsinitiative «St.Galler Klimafonds» und Gegenvorschlag",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Einheitsinitiative «St.Galler Klimafonds» und Gegenvorschlag") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Klimafonds") },
                PoliticalBusinessNumber = "01",
                ReviewProcedure = VoteReviewProcedure.Physically,
                ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
                ResultEntry = VoteResultEntry.FinalResults,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.BallotCreated
        {
            Ballot = new BallotEventData
            {
                Id = Vote1BallotId,
                Position = 1,
                VoteId = Vote1Id,
                BallotType = BallotType.VariantsBallot,
                BallotQuestions =
                {
                    new BallotQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Wollen Sie der Einheitsinitiative «St.Galler Klimafonds» zustimmen?") },
                        Type = BallotQuestionType.MainBallot,
                    },
                    new BallotQuestionEventData
                    {
                        Number = 2,
                        Question = { LanguageUtil.MockAllLanguages("Wollen Sie dem Gegenvorschlag des Kantonsrates in Form des Kantonsratsbeschlusses über den Sonderkredit zur Finanzierung der Energieförderung in den Jahren 2024 bis 2030 zustimmen?") },
                        Type = BallotQuestionType.CounterProposal,
                    },
                },
                TieBreakQuestions =
                {
                    new TieBreakQuestionEventData
                    {
                        Number = 1,
                        Question1Number = 1,
                        Question2Number = 2,
                        Question = { LanguageUtil.MockAllLanguages("Falls sowohl die Einheitsinitiative als auch der Gegenvorschlag angenommen werden: Geben Sie der Einheitsinitiative oder dem Gegenvorschlag den Vorzug?") },
                    },
                },
                HasTieBreakQuestions = true,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.VoteCreated
        {
            Vote = new VoteEventData
            {
                Id = Vote2Id,
                ContestId = ContestId,
                DomainOfInfluenceId = DomainOfInfluenceId,
                InternalDescription = "Nachtrag zum Gesetz über Beiträge für familien- und schulergänzende Kinderbetreuung",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Nachtrag zum Gesetz über Beiträge für familien- und schulergänzende Kinderbetreuung") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Nachtrag Kinderbetreung") },
                PoliticalBusinessNumber = "02",
                ReviewProcedure = VoteReviewProcedure.Physically,
                ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
                ResultEntry = VoteResultEntry.FinalResults,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.BallotCreated
        {
            Ballot = new BallotEventData
            {
                Id = Vote2BallotId,
                Position = 1,
                VoteId = Vote2Id,
                BallotType = BallotType.StandardBallot,
                BallotQuestions =
                {
                    new BallotQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Wollen Sie dem Nachtrag zum Gesetz über Beiträge für familien- und schulergänzende Kinderbetreuung zustimmen?") },
                        Type = BallotQuestionType.MainBallot,
                    },
                },
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.VoteCreated
        {
            Vote = new VoteEventData
            {
                Id = Vote3Id,
                ContestId = ContestId,
                DomainOfInfluenceId = DomainOfInfluenceId,
                InternalDescription = "Kantonsratsbeschluss über die Instandsetzung und Umnutzung der Schützengasse 1",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Kantonsratsbeschluss über die Instandsetzung und Umnutzung der Schützengasse 1") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Kantonsratsbeschluss") },
                PoliticalBusinessNumber = "03",
                ReviewProcedure = VoteReviewProcedure.Physically,
                ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
                ResultEntry = VoteResultEntry.FinalResults,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.BallotCreated
        {
            Ballot = new BallotEventData
            {
                Id = Vote3BallotId,
                Position = 1,
                VoteId = Vote3Id,
                BallotType = BallotType.StandardBallot,
                BallotQuestions =
                {
                    new BallotQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Wollen Sie dem Kantonsratsbeschluss über die Instandsetzung und Umnutzung der Schützengasse 1 in St.Gallen für das Kreisgericht St.Gallen zustimmen?") },
                        Type = BallotQuestionType.MainBallot,
                    },
                },
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.VoteActiveStateUpdated
        {
            VoteId = Vote1Id,
            Active = true,
            EventInfo = GetMockedBasisEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.VoteActiveStateUpdated
        {
            VoteId = Vote2Id,
            Active = true,
            EventInfo = GetMockedBasisEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.VoteActiveStateUpdated
        {
            VoteId = Vote3Id,
            Active = true,
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ContestTestingPhaseEnded
        {
            ContestId = ContestId,
            EventInfo = GetMockedBasisEventInfo(),
        });
    }

    private async Task EnterContestDetails()
    {
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
                    CountOfReceivedVotingCards = 2,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = true,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.BallotBox,
                    CountOfReceivedVotingCards = 4,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = true,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail,
                    CountOfReceivedVotingCards = 16,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = false,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail,
                    CountOfReceivedVotingCards = 5,
                },
            },
            CountOfVotersInformationSubTotals =
            {
                new UpdateCountOfVotersInformationSubTotalRequest
                {
                    CountOfVoters = 6171,
                    Sex = SexType.Male,
                    VoterType = VoterType.Swiss,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                },
                new UpdateCountOfVotersInformationSubTotalRequest
                {
                    CountOfVoters = 6180,
                    Sex = SexType.Female,
                    VoterType = VoterType.Swiss,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                },
            },
        });

        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyCreated>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ContestCountingCircleDetailsCreated>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private async Task ImportEVotingResults()
    {
        var uri = new Uri($"api/result_import/e-voting/{ContestId}", UriKind.RelativeOrAbsolute);
        using var httpClient = CreateHttpClient(RolesMockedData.MonitoringElectionAdmin);
        using var resp = await httpClient.PostFiles(
            uri,
            ("ech0222File", EVotingEch0222File),
            ("ech0110File", EVotingEch0110File));
        resp.EnsureSuccessStatusCode();

        EventPublisherMock.AllPublishedEvents.Should().HaveCount(8);
        EventPublisherMock.GetPublishedEvents<ResultImportCreated>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ResultImportStarted>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<CountingCircleVotingCardsImported>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<VoteResultImported>().Should().HaveCount(3);
        EventPublisherMock.GetPublishedEvents<ResultImportCompleted>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private async Task EnterResults()
    {
        // This starts the result submission
        var resultService = CreateService<ResultService.ResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
        await resultService.GetListAsync(new GetResultListRequest
        {
            ContestId = ContestId,
            CountingCircleId = CountingCircleId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(3);
        EventPublisherMock.GetPublishedEvents<VoteResultSubmissionStarted>().Should().HaveCount(3);
        await RunAllEvents();

        var voteResultService = CreateService<VoteResultService.VoteResultServiceClient>(
            RolesMockedData.ErfassungElectionAdmin);

        foreach (var voteResultId in VoteResultIds)
        {
            await voteResultService.DefineEntryAsync(new DefineVoteResultEntryRequest
            {
                VoteResultId = voteResultId,
                ResultEntry = Abraxas.Voting.Ausmittlung.Shared.V1.VoteResultEntry.FinalResults,
            });
        }

        EventPublisherMock.AllPublishedEvents.Should().HaveCount(3);
        EventPublisherMock.GetPublishedEvents<VoteResultEntryDefined>().Should().HaveCount(3);
        await RunAllEvents();

        await voteResultService.EnterResultsAsync(new EnterVoteResultsRequest
        {
            VoteResultId = Vote1ResultId,
            Results =
            {
                new EnterVoteBallotResultsRequest
                {
                    BallotId = Vote1BallotId,
                    CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                    {
                        ConventionalReceivedBallots = 22,
                        ConventionalBlankBallots = 3,
                        ConventionalInvalidBallots = 1,
                        ConventionalAccountedBallots = 18,
                    },
                    QuestionResults =
                    {
                        new EnterVoteBallotQuestionResultRequest
                        {
                            QuestionNumber = 1,
                            ReceivedCountYes = 15,
                            ReceivedCountNo = 2,
                            ReceivedCountUnspecified = 1,
                        },
                        new EnterVoteBallotQuestionResultRequest
                        {
                            QuestionNumber = 2,
                            ReceivedCountYes = 5,
                            ReceivedCountNo = 10,
                            ReceivedCountUnspecified = 3,
                        },
                    },
                    TieBreakQuestionResults =
                    {
                        new EnterVoteTieBreakQuestionResultRequest
                        {
                            QuestionNumber = 1,
                            ReceivedCountQ1 = 12,
                            ReceivedCountQ2 = 5,
                            ReceivedCountUnspecified = 1,
                        },
                    },
                },
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<VoteResultCountOfVotersEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<VoteResultEntered>().Should().HaveCount(1);
        await RunAllEvents();

        await voteResultService.EnterResultsAsync(new EnterVoteResultsRequest
        {
            VoteResultId = Vote2ResultId,
            Results =
            {
                new EnterVoteBallotResultsRequest
                {
                    BallotId = Vote2BallotId,
                    CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                    {
                        ConventionalReceivedBallots = 21,
                        ConventionalBlankBallots = 1,
                        ConventionalInvalidBallots = 2,
                        ConventionalAccountedBallots = 18,
                    },
                    QuestionResults =
                    {
                        new EnterVoteBallotQuestionResultRequest
                        {
                            QuestionNumber = 1,
                            ReceivedCountYes = 10,
                            ReceivedCountNo = 8,
                        },
                    },
                },
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<VoteResultCountOfVotersEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<VoteResultEntered>().Should().HaveCount(1);
        await RunAllEvents();

        await voteResultService.EnterResultsAsync(new EnterVoteResultsRequest
        {
            VoteResultId = Vote3ResultId,
            Results =
            {
                new EnterVoteBallotResultsRequest
                {
                    BallotId = Vote3BallotId,
                    CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                    {
                        ConventionalReceivedBallots = 19,
                        ConventionalBlankBallots = 0,
                        ConventionalInvalidBallots = 4,
                        ConventionalAccountedBallots = 15,
                    },
                    QuestionResults =
                    {
                        new EnterVoteBallotQuestionResultRequest
                        {
                            QuestionNumber = 1,
                            ReceivedCountYes = 2,
                            ReceivedCountNo = 13,
                        },
                    },
                },
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<VoteResultCountOfVotersEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<VoteResultEntered>().Should().HaveCount(1);
        await RunAllEvents();

        foreach (var voteResultId in VoteResultIds)
        {
            await voteResultService.SubmissionFinishedAsync(new VoteResultSubmissionFinishedRequest
            {
                VoteResultId = voteResultId,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            });
        }

        EventPublisherMock.AllPublishedEvents.Should().HaveCount(3);
        EventPublisherMock.GetPublishedEvents<VoteResultSubmissionFinished>().Should().HaveCount(3);
        await RunAllEvents();

        var monitoringResultService = CreateService<VoteResultService.VoteResultServiceClient>(RolesMockedData.MonitoringElectionAdmin);
        await monitoringResultService.AuditedTentativelyAsync(new VoteResultAuditedTentativelyRequest
        {
            VoteResultIds = { VoteResultIds },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(6);
        EventPublisherMock.GetPublishedEvents<VoteResultAuditedTentatively>().Should().HaveCount(3);
        EventPublisherMock.GetPublishedEvents<VoteResultPublished>().Should().HaveCount(3);
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

    private record VoteResultExpectedData(
        string VoteId,
        ExpectedVotingCards VotingCards,
        ExpectedCountOfVoters ConventionalCountOfVoters,
        ExpectedCountOfVoters EVotingCountOfVoters,
        ExpectedCountOfVoters TotalCountOfVoters,
        List<QuestionResult> QuestionResults,
        List<TieBreakQuestionResult> TieBreakQuestionResults);

    private record ExpectedVotingCards(
        int Paper,
        int BallotBox,
        int EVoting,
        int MailValid,
        int MailInvalid,
        int TotalValid,
        int TotalReceived);

    private record ExpectedCountOfVoters(
        int ReceivedBallots,
        int BlankBallots,
        int InvalidBallots,
        int AccountedBallots);

    private record QuestionResult(
        QuestionYesNoResult ConventionalCount,
        QuestionYesNoResult EVotingCount,
        QuestionYesNoResult TotalCount,
        bool Accepted);

    private record QuestionYesNoResult(
        int Yes,
        int No,
        int Unspecified);

    private record TieBreakQuestionResult(
        QuestionQ1Q2Result ConventionalCount,
        QuestionQ1Q2Result EVotingCount,
        QuestionQ1Q2Result TotalCount,
        bool Q1Accepted);

    private record QuestionQ1Q2Result(
        int Q1,
        int Q2,
        int Unspecified);
}
