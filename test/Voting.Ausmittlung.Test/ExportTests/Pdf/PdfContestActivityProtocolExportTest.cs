// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Google.Protobuf.WellKnownTypes;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Report.EventLogs.Aggregates;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using ProtoBasis = Abraxas.Voting.Basis.Events.V1;
using ProtoBasisEvents = Abraxas.Voting.Basis.Events.V1.Data;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfContestActivityProtocolExportTest : PdfContestActivityProtocolExportBaseTest
{
    public PdfContestActivityProtocolExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override Task TestPdf()
    {
        SeedEvents(false);
        return base.TestPdf();
    }

    public override Task TestPdfAfterTestingPhaseEnded()
    {
        SeedEvents(true);
        return base.TestPdfAfterTestingPhaseEnded();
    }

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private void SeedEvents(bool testingPhaseEnded)
    {
        SeedCountingCircleInitEvents();
        SeedContestInitEvents();

        SeedBasisPublicKeySignatureEvents();
        SeedAusmittlungPublicKeySignatureEvents();

        SeedVoteInitEvents();
        SeedProportionalElectionInitEvents();
        SeedMajorityElectionInitEvents();

        SeedProportionalElectionUnionInitEvents();
        SeedMajorityElectionUnionInitEvents();

        if (testingPhaseEnded)
        {
            SeedContestTestingPhaseEndedEvent();
        }

        SeedContestCountingCircleDetailsEvents();
        SeedMajorityElectionResultEvents();
        SeedProportionalElectionResultEvents();
        SeedVoteResultEvents();
        SeedExportEvents();
    }

    private void SeedCountingCircleInitEvents()
    {
        var protoCountingCircleId = CountingCircleId.ToString();
        var countingCircleCreated = new ProtoBasis.CountingCircleCreated
        {
            EventInfo = GetBasisEventInfo(0),
            CountingCircle = new()
            {
                Id = protoCountingCircleId,
                Name = "Gossau",
                Bfs = "3443",
            },
        };

        PublishBasisBusinessEvent(countingCircleCreated, protoCountingCircleId);
    }

    private void SeedContestInitEvents()
    {
        var contestCreated = new ProtoBasis.ContestCreated
        {
            EventInfo = GetBasisEventInfo(0),
            Contest = new ProtoBasisEvents.ContestEventData
            {
                Id = ContestId,
                Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("Contest description") },
                EndOfTestingPhase = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            },
        };

        PublishBasisBusinessEvent(contestCreated, contestCreated.Contest.Id, Host1, BasisKeyHost1, ContestIdGuid, aggregateName: AggregateNames.Contest);
    }

    private void SeedVoteInitEvents()
    {
        var vote = VoteMockedData.StGallenVoteInContestBund;
        var voteId = vote.Id.ToString();

        var voteCreated = new ProtoBasis.VoteCreated
        {
            EventInfo = GetBasisEventInfo(0),
            Vote = new()
            {
                Id = voteId,
                PoliticalBusinessNumber = vote.PoliticalBusinessNumber,
                ShortDescription = { vote.Translations.ToDictionary(x => x.Language, x => x.ShortDescription) },
            },
        };

        PublishBasisBusinessEvent(voteCreated, voteId, Host1, BasisKeyHost1, ContestIdGuid);

        foreach (var ballot in vote.Ballots)
        {
            var ballotCreated = new ProtoBasis.BallotCreated
            {
                EventInfo = GetBasisEventInfo(0),
                Ballot = new()
                {
                    Id = ballot.Id.ToString(),
                    VoteId = voteId,
                },
            };
            PublishBasisBusinessEvent(ballotCreated, voteId, Host1, BasisKeyHost1, ContestIdGuid);
        }
    }

    private void SeedProportionalElectionInitEvents()
    {
        var election = ProportionalElectionMockedData.StGallenProportionalElectionInContestBund;
        var electionId = election.Id.ToString();

        var electionCreated = new ProtoBasis.ProportionalElectionCreated
        {
            EventInfo = GetBasisEventInfo(0),
            ProportionalElection = new()
            {
                Id = electionId,
                PoliticalBusinessNumber = election.PoliticalBusinessNumber,
                ShortDescription = { election.Translations.ToDictionary(x => x.Language, x => x.ShortDescription) },
            },
        };

        PublishBasisBusinessEvent(electionCreated, electionId, Host1, BasisKeyHost1, ContestIdGuid);

        foreach (var list in election.ProportionalElectionLists)
        {
            var listId = list.Id.ToString();
            var listCreated = new ProtoBasis.ProportionalElectionListCreated
            {
                EventInfo = GetBasisEventInfo(0),
                ProportionalElectionList = new()
                {
                    Id = listId,
                    ProportionalElectionId = electionId,
                },
            };

            PublishBasisBusinessEvent(listCreated, electionId, Host1, BasisKeyHost1, ContestIdGuid);

            foreach (var candidate in list.ProportionalElectionCandidates)
            {
                var candidateCreated = new ProtoBasis.ProportionalElectionCandidateCreated
                {
                    EventInfo = GetBasisEventInfo(0),
                    ProportionalElectionCandidate = new()
                    {
                        Id = candidate.Id.ToString(),
                        ProportionalElectionListId = listId,
                        ProportionalElectionId = electionId,
                    },
                };

                PublishBasisBusinessEvent(candidateCreated, electionId, Host1, BasisKeyHost1, ContestIdGuid);
            }
        }
    }

    private void SeedMajorityElectionInitEvents()
    {
        var election = MajorityElectionMockedData.StGallenMajorityElectionInContestBund;
        var electionId = election.Id.ToString();

        var electionCreated = new ProtoBasis.MajorityElectionCreated
        {
            EventInfo = GetBasisEventInfo(0),
            MajorityElection = new()
            {
                Id = electionId,
                PoliticalBusinessNumber = election.PoliticalBusinessNumber,
                ShortDescription = { election.Translations.ToDictionary(x => x.Language, x => x.ShortDescription) },
            },
        };

        PublishBasisBusinessEvent(electionCreated, electionId, Host1, BasisKeyHost1, ContestIdGuid);

        foreach (var candidate in election.MajorityElectionCandidates)
        {
            var candidateCreated = new ProtoBasis.MajorityElectionCandidateCreated
            {
                EventInfo = GetBasisEventInfo(0),
                MajorityElectionCandidate = new()
                {
                    Id = candidate.Id.ToString(),
                    MajorityElectionId = electionId,
                },
            };

            PublishBasisBusinessEvent(candidateCreated, electionId, Host1, BasisKeyHost1, ContestIdGuid);
        }

        foreach (var secondaryMajorityElection in election.SecondaryMajorityElections)
        {
            var smeId = secondaryMajorityElection.Id.ToString();
            var smeCreated = new ProtoBasis.SecondaryMajorityElectionCreated
            {
                EventInfo = GetBasisEventInfo(0),
                SecondaryMajorityElection = new()
                {
                    Id = smeId,
                    PrimaryMajorityElectionId = electionId,
                    PoliticalBusinessNumber = secondaryMajorityElection.PoliticalBusinessNumber,
                    ShortDescription = { secondaryMajorityElection.Translations.ToDictionary(x => x.Language, x => x.ShortDescription) },
                },
            };

            PublishBasisBusinessEvent(smeCreated, electionId, Host1, BasisKeyHost1, ContestIdGuid);

            foreach (var smeCandidate in secondaryMajorityElection.Candidates)
            {
                var smeCandidateId = smeCandidate.Id.ToString();

                if (!smeCandidate.CandidateReferenceId.HasValue)
                {
                    var smeCandidateCreated = new ProtoBasis.SecondaryMajorityElectionCandidateCreated
                    {
                        EventInfo = GetBasisEventInfo(0),
                        SecondaryMajorityElectionCandidate = new()
                        {
                            Id = smeCandidateId,
                            MajorityElectionId = smeId,
                        },
                    };

                    PublishBasisBusinessEvent(smeCandidateCreated, electionId, Host1, BasisKeyHost1, ContestIdGuid);
                }
                else
                {
                    var smeCandidateRefCreated = new ProtoBasis.SecondaryMajorityElectionCandidateReferenceCreated
                    {
                        EventInfo = GetBasisEventInfo(0),
                        MajorityElectionCandidateReference = new()
                        {
                            Id = smeCandidateId,
                            CandidateId = smeCandidate.CandidateReferenceId.Value.ToString(),
                            SecondaryMajorityElectionId = smeId,
                            PrimaryMajorityElectionId = electionId,
                        },
                    };

                    PublishBasisBusinessEvent(smeCandidateRefCreated, electionId, Host1, BasisKeyHost1, ContestIdGuid);
                }
            }
        }
    }

    private void SeedProportionalElectionUnionInitEvents()
    {
        var unionId = "5233cf7c-2200-435c-b5d6-8d354794de4c";

        var electionUnionCreated = new ProtoBasis.ProportionalElectionUnionCreated
        {
            EventInfo = GetBasisEventInfo(0),
            ProportionalElectionUnion = new()
            {
                ContestId = ContestId,
                Id = unionId,
                Description = "Proportional Election Union",
            },
        };

        PublishBasisBusinessEvent(electionUnionCreated, unionId, Host1, BasisKeyHost1, ContestIdGuid);

        var electionUnionUpdated = new ProtoBasis.ProportionalElectionUnionUpdated
        {
            EventInfo = GetBasisEventInfo(0),
            ProportionalElectionUnion = new()
            {
                ContestId = ContestId,
                Id = unionId,
                Description = "Proportional Election Union Updated",
            },
        };

        PublishBasisBusinessEvent(electionUnionUpdated, unionId, Host1, BasisKeyHost1, ContestIdGuid);
    }

    private void SeedMajorityElectionUnionInitEvents()
    {
        var unionId = "5ef889b6-7227-4bc1-8a1c-99011d48bead";

        var electionUnionCreated = new ProtoBasis.MajorityElectionUnionCreated
        {
            EventInfo = GetBasisEventInfo(0),
            MajorityElectionUnion = new()
            {
                ContestId = ContestId,
                Id = unionId,
                Description = "Majority Election Union",
            },
        };

        PublishBasisBusinessEvent(electionUnionCreated, unionId, Host1, BasisKeyHost1, ContestIdGuid);

        var electionUnionUpdated = new ProtoBasis.MajorityElectionUnionUpdated
        {
            EventInfo = GetBasisEventInfo(0),
            MajorityElectionUnion = new()
            {
                ContestId = ContestId,
                Id = unionId,
                Description = "Majority Election Union Updated",
            },
        };

        PublishBasisBusinessEvent(electionUnionUpdated, unionId, Host1, BasisKeyHost1, ContestIdGuid);
    }

    private void SeedContestTestingPhaseEndedEvent()
    {
        var contestTestingPhaseEnded = new ProtoBasis.ContestTestingPhaseEnded
        {
            EventInfo = GetBasisEventInfo(1),
            ContestId = ContestId,
        };

        PublishBasisBusinessEvent(contestTestingPhaseEnded, contestTestingPhaseEnded.ContestId, Host1, BasisKeyHost1AfterTestingPhaseEnded, ContestIdGuid, AggregateNames.Contest);
    }

    private void SeedContestCountingCircleDetailsEvents()
    {
        var contestCountingCircleDetailsEvent = new ContestCountingCircleDetailsCreated
        {
            Id = "c9679b05-64ab-46ab-9f4e-f62ed89a4968",
            EventInfo = GetEventInfo(1),
            ContestId = ContestId,
            CountingCircleId = CountingCircleId.ToString(),
            CountOfVotersInformation = new CountOfVotersInformationEventData
            {
                SubTotalInfo =
                    {
                        new CountOfVotersInformationSubTotalEventData
                        {
                            CountOfVoters = 700,
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.Swiss,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            CountOfVoters = 500,
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.Swiss,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            CountOfVoters = 100,
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.SwissAbroad,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            CountOfVoters = 50,
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.SwissAbroad,
                        },
                    },
                TotalCountOfVoters = 1350,
            },
        };

        PublishAusmittlungBusinessEvent(contestCountingCircleDetailsEvent, contestCountingCircleDetailsEvent.Id, Host1, AusmittlungKeyHost1);

        var contestCountingCircleDetailsUpdateEvent = new ContestCountingCircleDetailsUpdated
        {
            EventInfo = GetEventInfo(2),
            ContestId = ContestId,
            CountingCircleId = CountingCircleId.ToString(),
            Id = "c9679b05-64ab-46ab-9f4e-f62ed89a4968",
            CountOfVotersInformation = new CountOfVotersInformationEventData
            {
                SubTotalInfo =
                    {
                        new CountOfVotersInformationSubTotalEventData
                        {
                            CountOfVoters = 800,
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.Swiss,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            CountOfVoters = 500,
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.Swiss,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            CountOfVoters = 100,
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.SwissAbroad,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            CountOfVoters = 50,
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.SwissAbroad,
                        },
                    },
                TotalCountOfVoters = 1450,
            },
        };

        PublishAusmittlungBusinessEvent(contestCountingCircleDetailsUpdateEvent, contestCountingCircleDetailsUpdateEvent.Id, Host2, AusmittlungKeyHost2);
    }

    private void SeedMajorityElectionResultEvents()
    {
        var resultId = MajorityElectionEndResultMockedData.StGallenResultId;
        var electionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund;

        var submissionStartedEvent = new MajorityElectionResultSubmissionStarted
        {
            EventInfo = GetEventInfo(10),
            CountingCircleId = CountingCircleId.ToString(),
            ElectionId = electionId,
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(submissionStartedEvent, resultId, Host1, AusmittlungKeyHost1);

        var entryDefinedEvent = new MajorityElectionResultEntryDefined
        {
            EventInfo = GetEventInfo(12),
            ElectionResultId = resultId,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new MajorityElectionResultEntryParamsEventData
            {
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 3,
                BallotBundleSize = 25,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        };
        PublishAusmittlungBusinessEvent(entryDefinedEvent, resultId, Host1, AusmittlungKeyHost1);

        var countOfVotersEnteredEvent = new MajorityElectionResultCountOfVotersEntered
        {
            EventInfo = GetEventInfo(13),
            ElectionResultId = resultId,
            CountOfVoters = new PoliticalBusinessCountOfVotersEventData
            {
                ConventionalReceivedBallots = 500,
                ConventionalBlankBallots = 10,
                ConventionalInvalidBallots = 20,
                ConventionalAccountedBallots = 470,
            },
        };
        PublishAusmittlungBusinessEvent(countOfVotersEnteredEvent, resultId, Host1, AusmittlungKeyHost1);

        var candidateResultsEntered = new MajorityElectionCandidateResultsEntered
        {
            EventInfo = GetEventInfo(15),
            ElectionResultId = resultId,
            InvalidVoteCount = 3,
            EmptyVoteCount = 5,
            IndividualVoteCount = 13,
            CandidateResults =
                {
                    new MajorityElectionCandidateResultCountEventData
                    {
                        CandidateId = "0cfd246d-eb90-412e-b4f8-c5ce26b1b58b",
                        VoteCount = 17,
                    },
                    new MajorityElectionCandidateResultCountEventData
                    {
                        CandidateId = "163bb2ac-35c0-4ca6-995a-8af1a6529dd9",
                        VoteCount = 23,
                    },
                },
            SecondaryElectionCandidateResults =
                {
                    new SecondaryMajorityElectionCandidateResultsEventData
                    {
                        EmptyVoteCount = 10,
                        IndividualVoteCount = 5,
                        InvalidVoteCount = 3,
                        SecondaryMajorityElectionId = "06e4ac05-0e7e-4629-ae81-aef5c648d900",
                        CandidateResults =
                        {
                            new MajorityElectionCandidateResultCountEventData
                            {
                                CandidateId = "c18d1ba5-e059-40ca-9c36-fbfff1ccfdb6",
                                VoteCount = 18,
                            },
                            new MajorityElectionCandidateResultCountEventData
                            {
                                CandidateId = "bb1cbbf8-09d6-44d0-bb1f-4e2fb8e560c0",
                                VoteCount = 12,
                            },
                        },
                    },
                    new SecondaryMajorityElectionCandidateResultsEventData
                    {
                        EmptyVoteCount = 5,
                        IndividualVoteCount = 7,
                        InvalidVoteCount = 2,
                        SecondaryMajorityElectionId = "438d4e45-e988-4d73-9620-13c4f6393f6b",
                        CandidateResults =
                        {
                            new MajorityElectionCandidateResultCountEventData
                            {
                                CandidateId = "c18d1ba5-e059-40ca-9c36-fbfff1ccfdb6",
                                VoteCount = 18,
                            },
                            new MajorityElectionCandidateResultCountEventData
                            {
                                CandidateId = "bb1cbbf8-09d6-44d0-bb1f-4e2fb8e560c0",
                                VoteCount = 12,
                            },
                            new MajorityElectionCandidateResultCountEventData
                            {
                                CandidateId = "a8773d00-de9b-41b4-8eaa-8b3db7da9dfe",
                                VoteCount = 3,
                            },
                        },
                    },
                },
        };
        PublishAusmittlungBusinessEvent(candidateResultsEntered, resultId, Host1, AusmittlungKeyHost1);

        var ballotGroupResultsEntered = new MajorityElectionBallotGroupResultsEntered
        {
            EventInfo = GetEventInfo(17),
            ElectionResultId = resultId,
            Results =
                {
                    new MajorityElectionBallotGroupResultEventData
                    {
                        BallotGroupId = "0b3776b2-0599-445f-8373-a007414c7fc7",
                        VoteCount = 8,
                    },
                    new MajorityElectionBallotGroupResultEventData
                    {
                        BallotGroupId = "d80b086a-8332-46c4-98b0-b6c209ed87a9",
                        VoteCount = 35,
                    },
                },
        };
        PublishAusmittlungBusinessEvent(ballotGroupResultsEntered, resultId, Host1, AusmittlungKeyHost1);

        SeedMajorityElectionBundleEvents();

        var submissionFinished = new MajorityElectionResultSubmissionFinished
        {
            EventInfo = GetEventInfo(223),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(submissionFinished, resultId, Host1, AusmittlungKeyHost1);

        SeedMajorityElectionAfterTestingPhaseEndedEvents();

        var flaggedForCorrection = new MajorityElectionResultFlaggedForCorrection
        {
            EventInfo = GetEventInfo(226, true),
            ElectionResultId = resultId,
            Comment = "Bitte korrigieren",
        };
        PublishAusmittlungBusinessEvent(flaggedForCorrection, resultId, Host1, AusmittlungKeyHost1);

        var correctionFinished = new MajorityElectionResultCorrectionFinished
        {
            EventInfo = GetEventInfo(230),
            ElectionResultId = resultId,
            Comment = "Korrigiert",
        };
        PublishAusmittlungBusinessEvent(correctionFinished, resultId, Host1, AusmittlungKeyHost1);

        var auditedTentatively = new MajorityElectionResultAuditedTentatively
        {
            EventInfo = GetEventInfo(236, true),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(auditedTentatively, resultId, Host1, AusmittlungKeyHost1);

        var plausibilised = new MajorityElectionResultPlausibilised
        {
            EventInfo = GetEventInfo(340, true),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(plausibilised, resultId, Host1, AusmittlungKeyHost1);

        var resettedAuditedTentatively = new MajorityElectionResultResettedToAuditedTentatively
        {
            EventInfo = GetEventInfo(440, true),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(resettedAuditedTentatively, resultId, Host1, AusmittlungKeyHost1);

        var resetted = new MajorityElectionResultResettedToSubmissionFinished
        {
            EventInfo = GetEventInfo(445, true),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(resetted, resultId, Host1, AusmittlungKeyHost1);

        var lotDecisionsUpdated = new MajorityElectionEndResultLotDecisionsUpdated
        {
            EventInfo = GetEventInfo(660, true),
            MajorityElectionEndResultId = "c37becfb-4444-480d-986a-89081b0772fc",
            MajorityElectionId = electionId,
            LotDecisions =
                {
                    new MajorityElectionEndResultLotDecisionEventData
                    {
                        CandidateId = "abd84dd7-a31e-4511-a64d-9d60c27de0fe",
                        Rank = 1,
                    },
                    new MajorityElectionEndResultLotDecisionEventData
                    {
                        CandidateId = "b4b3f660-dde3-4e0f-b4bd-02118b798bcd",
                        Rank = 2,
                    },
                },
        };
        PublishAusmittlungBusinessEvent(lotDecisionsUpdated, lotDecisionsUpdated.MajorityElectionEndResultId, Host1, AusmittlungKeyHost1);
    }

    private void SeedMajorityElectionBundleEvents()
    {
        var resultId = MajorityElectionEndResultMockedData.StGallenResultId;
        var bundleId = "a3306f28-bf8d-400f-935f-c56c50fcdb96";
        var ballotNumber = 1;

        var bundleCreated = new MajorityElectionResultBundleCreated
        {
            EventInfo = GetEventInfo(100),
            ElectionResultId = resultId,
            BundleId = bundleId,
            BundleNumber = 1,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new MajorityElectionResultEntryParamsEventData
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        };
        PublishAusmittlungBusinessEvent(bundleCreated, resultId, Host2, AusmittlungKeyHost2);

        var ballotCreated = new MajorityElectionResultBallotCreated
        {
            EventInfo = GetEventInfo(105),
            ElectionResultId = resultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
            EmptyVoteCount = 1,
            IndividualVoteCount = 13,
            InvalidVoteCount = 2,
            SelectedCandidateIds = { "d6f1ced5-229a-4c4e-bb48-846e593a3d3b", "7b3beea0-bba7-4d21-84a6-faac4557d366" },
            SecondaryMajorityElectionResults =
                {
                    new SecondaryMajorityElectionResultBallotEventData
                    {
                        EmptyVoteCount = 10,
                        IndividualVoteCount = 3,
                        InvalidVoteCount = 2,
                        SecondaryMajorityElectionId = "f54a632c-c1eb-4a51-a729-d37d2dc26cb6",
                        SelectedCandidateIds = { "abec9b43-e7d4-4fad-ab3b-098dd79badbf", "4f7ddbcd-64b5-4b22-a606-9f91882fd2df" },
                    },
                    new SecondaryMajorityElectionResultBallotEventData
                    {
                        EmptyVoteCount = 3,
                        IndividualVoteCount = 1,
                        InvalidVoteCount = 3,
                        SecondaryMajorityElectionId = "2eed5a2c-fe25-4804-88fb-ab620287862d",
                        SelectedCandidateIds = { "078ef959-d904-4a71-b48f-a157f7541d5a", "7b3beea0-bba7-4d21-84a6-faac4557d366" },
                    },
                },
        };
        PublishAusmittlungBusinessEvent(ballotCreated, resultId, Host2, AusmittlungKeyHost2);

        var ballotUpdated = new MajorityElectionResultBallotUpdated
        {
            EventInfo = GetEventInfo(110),
            ElectionResultId = resultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
            EmptyVoteCount = 1,
            IndividualVoteCount = 13,
            InvalidVoteCount = 2,
            SelectedCandidateIds = { "d6f1ced5-229a-4c4e-bb48-846e593a3d3b", "7b3beea0-bba7-4d21-84a6-faac4557d366" },
            SecondaryMajorityElectionResults =
                {
                    new SecondaryMajorityElectionResultBallotEventData
                    {
                        EmptyVoteCount = 10,
                        IndividualVoteCount = 3,
                        InvalidVoteCount = 2,
                        SecondaryMajorityElectionId = "f54a632c-c1eb-4a51-a729-d37d2dc26cb6",
                        SelectedCandidateIds = { "abec9b43-e7d4-4fad-ab3b-098dd79badbf", "4f7ddbcd-64b5-4b22-a606-9f91882fd2df" },
                    },
                    new SecondaryMajorityElectionResultBallotEventData
                    {
                        EmptyVoteCount = 3,
                        IndividualVoteCount = 1,
                        InvalidVoteCount = 3,
                        SecondaryMajorityElectionId = "2eed5a2c-fe25-4804-88fb-ab620287862d",
                        SelectedCandidateIds = { "078ef959-d904-4a71-b48f-a157f7541d5a", "7b3beea0-bba7-4d21-84a6-faac4557d366" },
                    },
                },
        };
        PublishAusmittlungBusinessEvent(ballotUpdated, resultId, Host2, AusmittlungKeyHost2);

        var ballotDeleted = new MajorityElectionResultBallotDeleted
        {
            EventInfo = GetEventInfo(111),
            ElectionResultId = resultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
        };
        PublishAusmittlungBusinessEvent(ballotDeleted, resultId, Host2, AusmittlungKeyHost2);

        var submissionFinished = new MajorityElectionResultBundleSubmissionFinished
        {
            EventInfo = GetEventInfo(115),
            ElectionResultId = resultId,
            BundleId = bundleId,
            SampleBallotNumbers = { 1, 3 },
        };
        PublishAusmittlungBusinessEvent(submissionFinished, resultId, Host2, AusmittlungKeyHost2);

        var reviewRejected = new MajorityElectionResultBundleReviewRejected
        {
            EventInfo = GetEventInfo(128),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(reviewRejected, resultId, Host2, AusmittlungKeyHost2);

        var correctionFinished = new MajorityElectionResultBundleCorrectionFinished
        {
            EventInfo = GetEventInfo(130),
            BundleId = bundleId,
            ElectionResultId = "0c15752f-9213-46ff-aeff-bf3bf11e9837",
            SampleBallotNumbers = { 1, 3 },
        };
        PublishAusmittlungBusinessEvent(correctionFinished, resultId, Host2, AusmittlungKeyHost2);

        var reviewSucceeded = new MajorityElectionResultBundleReviewSucceeded
        {
            EventInfo = GetEventInfo(140),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(reviewSucceeded, resultId, Host2, AusmittlungKeyHost2);

        var bundleDeleted = new MajorityElectionResultBundleDeleted
        {
            EventInfo = GetEventInfo(141),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(bundleDeleted, resultId, Host2, AusmittlungKeyHost2);
    }

    private void SeedProportionalElectionResultEvents()
    {
        var resultId = ProportionalElectionEndResultMockedData.StGallenResultId;
        var electionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund;

        var submissionStartedEvent = new ProportionalElectionResultSubmissionStarted
        {
            EventInfo = GetEventInfo(1010),
            CountingCircleId = CountingCircleId.ToString(),
            ElectionId = electionId,
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(submissionStartedEvent, resultId);

        var entryDefinedEvent = new ProportionalElectionResultEntryDefined
        {
            EventInfo = GetEventInfo(1012),
            ElectionResultId = resultId,
            ResultEntryParams = new ProportionalElectionResultEntryParamsEventData
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
        };
        PublishAusmittlungBusinessEvent(entryDefinedEvent, resultId);

        var countOfVotersEnteredEvent = new ProportionalElectionResultCountOfVotersEntered
        {
            EventInfo = GetEventInfo(1013),
            ElectionResultId = resultId,
            CountOfVoters = new PoliticalBusinessCountOfVotersEventData
            {
                ConventionalReceivedBallots = 500,
                ConventionalBlankBallots = 10,
                ConventionalInvalidBallots = 20,
                ConventionalAccountedBallots = 470,
            },
        };
        PublishAusmittlungBusinessEvent(countOfVotersEnteredEvent, resultId);

        var listResultsEntered = new ProportionalElectionUnmodifiedListResultsEntered
        {
            EventInfo = GetEventInfo(1015),
            ElectionResultId = resultId,
            Results =
                {
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        ListId = "a3e5027e-81c5-4835-b96a-1fe0ee741a4e",
                        VoteCount = 3,
                    },
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        ListId = "78e59407-07b4-4f96-af8a-0ff8eeeec377",
                        VoteCount = 5,
                    },
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        ListId = "7d203c37-29dc-4c77-9267-d8d9427134b6",
                        VoteCount = 16,
                    },
                },
        };
        PublishAusmittlungBusinessEvent(listResultsEntered, resultId);

        SeedProportionalElectionBundleEvents();

        var submissionFinished = new ProportionalElectionResultSubmissionFinished
        {
            EventInfo = GetEventInfo(1223),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(submissionFinished, resultId);

        SeedProportionalElectionAfterTestingPhaseEndedEvents();

        var flaggedForCorrection = new ProportionalElectionResultFlaggedForCorrection
        {
            EventInfo = GetEventInfo(1226, true),
            ElectionResultId = resultId,
            Comment = "Bitte korrigieren",
        };
        PublishAusmittlungBusinessEvent(flaggedForCorrection, resultId);

        var correctionFinished = new ProportionalElectionResultCorrectionFinished
        {
            EventInfo = GetEventInfo(1230),
            ElectionResultId = resultId,
            Comment = "Korrigiert",
        };
        PublishAusmittlungBusinessEvent(correctionFinished, resultId);

        var auditedTentatively = new ProportionalElectionResultAuditedTentatively
        {
            EventInfo = GetEventInfo(1236, true),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(auditedTentatively, resultId);

        var plausibilised = new ProportionalElectionResultPlausibilised
        {
            EventInfo = GetEventInfo(1340, true),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(plausibilised, resultId);

        var resettedAuditedTentatively = new ProportionalElectionResultResettedToAuditedTentatively
        {
            EventInfo = GetEventInfo(1443, true),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(resettedAuditedTentatively, resultId);

        var resetted = new ProportionalElectionResultResettedToSubmissionFinished
        {
            EventInfo = GetEventInfo(1445, true),
            ElectionResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(resetted, resultId);

        var lotDecisionsUpdated = new ProportionalElectionListEndResultLotDecisionsUpdated
        {
            EventInfo = GetEventInfo(1660, true),
            ProportionalElectionId = electionId,
            ProportionalElectionEndResultId = "cbd87126-6df5-4839-ab24-02fe42c9b27f",
            ProportionalElectionListId = "c322cf7f-9319-4886-9779-bf91c716a28a",
            LotDecisions =
                {
                    new ProportionalElectionEndResultLotDecisionEventData
                    {
                        CandidateId = "84c8360d-74b6-47f9-8b8b-926fe285c926",
                        Rank = 1,
                    },
                    new ProportionalElectionEndResultLotDecisionEventData
                    {
                        CandidateId = "2361dba0-df64-4183-bf23-b653018f6e4a",
                        Rank = 2,
                    },
                },
        };
        PublishAusmittlungBusinessEvent(lotDecisionsUpdated, lotDecisionsUpdated.ProportionalElectionEndResultId);
    }

    private void SeedProportionalElectionBundleEvents()
    {
        var resultId = ProportionalElectionEndResultMockedData.StGallenResultId;
        var bundleId = "21845802-45b0-4b17-9b42-6c65610a2f97";
        var ballotNumber = 1;

        var bundleCreated = new ProportionalElectionResultBundleCreated
        {
            EventInfo = GetEventInfo(1100),
            ElectionResultId = resultId,
            BundleId = bundleId,
            BundleNumber = 1,
            ListId = "f831b3f8-0a5f-4461-9e0a-fa2d96363089",
            ResultEntryParams = new ProportionalElectionResultEntryParamsEventData
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 3,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
        };
        PublishAusmittlungBusinessEvent(bundleCreated, bundleId);

        var ballotCreated = new ProportionalElectionResultBallotCreated
        {
            EventInfo = GetEventInfo(1105),
            ElectionResultId = resultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
            EmptyVoteCount = 1,
            Candidates =
                {
                    new ProportionalElectionResultBallotUpdatedCandidateEventData
                    {
                        CandidateId = "041b946d-789e-4ec9-863b-1cc70b295bab",
                        OnList = true,
                        Position = 1,
                    },
                    new ProportionalElectionResultBallotUpdatedCandidateEventData
                    {
                        CandidateId = "777c1b43-195f-451e-a834-b9ec922ff0d3",
                        OnList = false,
                        Position = 2,
                    },
                    new ProportionalElectionResultBallotUpdatedCandidateEventData
                    {
                        CandidateId = "e778679f-2ac8-4fcb-9e07-aa02d261f97d",
                        OnList = false,
                        Position = 3,
                    },
                },
        };
        PublishAusmittlungBusinessEvent(ballotCreated, bundleId);

        var ballotUpdated = new ProportionalElectionResultBallotUpdated
        {
            EventInfo = GetEventInfo(1110),
            ElectionResultId = resultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
            EmptyVoteCount = 1,
            Candidates =
                {
                    new ProportionalElectionResultBallotUpdatedCandidateEventData
                    {
                        CandidateId = "041b946d-789e-4ec9-863b-1cc70b295bab",
                        OnList = true,
                        Position = 1,
                    },
                    new ProportionalElectionResultBallotUpdatedCandidateEventData
                    {
                        CandidateId = "6777cbfd-70ae-4fb9-94c0-05401f4ad5a3",
                        OnList = false,
                        Position = 2,
                    },
                    new ProportionalElectionResultBallotUpdatedCandidateEventData
                    {
                        CandidateId = "e778679f-2ac8-4fcb-9e07-aa02d261f97d",
                        OnList = false,
                        Position = 3,
                    },
                },
        };
        PublishAusmittlungBusinessEvent(ballotUpdated, bundleId);

        var ballotDeleted = new ProportionalElectionResultBallotDeleted
        {
            EventInfo = GetEventInfo(1111),
            ElectionResultId = resultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
        };
        PublishAusmittlungBusinessEvent(ballotDeleted, bundleId);

        var submissionFinished = new ProportionalElectionResultBundleSubmissionFinished
        {
            EventInfo = GetEventInfo(1115),
            ElectionResultId = resultId,
            BundleId = bundleId,
            SampleBallotNumbers = { 1, 3 },
        };
        PublishAusmittlungBusinessEvent(submissionFinished, bundleId);

        var reviewRejected = new ProportionalElectionResultBundleReviewRejected
        {
            EventInfo = GetEventInfo(1128),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(reviewRejected, bundleId);

        var correctionFinished = new ProportionalElectionResultBundleCorrectionFinished
        {
            EventInfo = GetEventInfo(1130),
            ElectionResultId = resultId,
            BundleId = bundleId,
            SampleBallotNumbers = { 1, 3 },
        };
        PublishAusmittlungBusinessEvent(correctionFinished, bundleId);

        var reviewSucceeded = new ProportionalElectionResultBundleReviewSucceeded
        {
            EventInfo = GetEventInfo(1140),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(reviewSucceeded, bundleId);

        var bundleDeleted = new ProportionalElectionResultBundleDeleted
        {
            EventInfo = GetEventInfo(1144),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(bundleDeleted, bundleId);
    }

    private void SeedVoteResultEvents()
    {
        var resultId = "d453bc14-b433-4394-95af-4121ffa8674e";
        var voteId = VoteMockedData.IdStGallenVoteInContestBund;

        var submissionStartedEvent = new VoteResultSubmissionStarted
        {
            EventInfo = GetEventInfo(2010),
            CountingCircleId = CountingCircleId.ToString(),
            VoteResultId = resultId,
            VoteId = voteId,
        };
        PublishAusmittlungBusinessEvent(submissionStartedEvent, resultId);

        var entryDefinedEvent = new VoteResultEntryDefined
        {
            EventInfo = GetEventInfo(2012),
            VoteResultId = resultId,
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
        };
        PublishAusmittlungBusinessEvent(entryDefinedEvent, resultId);

        var countOfVotersEnteredEvent = new VoteResultCountOfVotersEntered
        {
            EventInfo = GetEventInfo(2013),
            VoteResultId = resultId,
            ResultsCountOfVoters =
                {
                    new VoteBallotResultsCountOfVotersEventData
                    {
                        BallotId = "6f4f9d65-9265-4b68-9e89-e94e7afbd097",
                        CountOfVoters = new PoliticalBusinessCountOfVotersEventData
                        {
                            ConventionalReceivedBallots = 500,
                            ConventionalBlankBallots = 10,
                            ConventionalInvalidBallots = 20,
                            ConventionalAccountedBallots = 470,
                        },
                    },
                },
        };
        PublishAusmittlungBusinessEvent(countOfVotersEnteredEvent, resultId);

        var resultEntered = new VoteResultEntered
        {
            EventInfo = GetEventInfo(2014),
            VoteResultId = resultId,
            Results =
                {
                    new VoteBallotResultsEventData
                    {
                        BallotId = "6f4f9d65-9265-4b68-9e89-e94e7afbd097",
                        QuestionResults =
                        {
                            new VoteBallotQuestionResultsEventData
                            {
                                QuestionNumber = 1,
                                ReceivedCountNo = 50,
                                ReceivedCountYes = 120,
                                ReceivedCountUnspecified = 3,
                            },
                            new VoteBallotQuestionResultsEventData
                            {
                                QuestionNumber = 2,
                                ReceivedCountNo = 25,
                                ReceivedCountYes = 10,
                                ReceivedCountUnspecified = 3,
                            },
                        },
                        TieBreakQuestionResults =
                        {
                            new VoteTieBreakQuestionResultsEventData
                            {
                                QuestionNumber = 1,
                                ReceivedCountQ1 = 10,
                                ReceivedCountQ2 = 25,
                                ReceivedCountUnspecified = 7,
                            },
                            new VoteTieBreakQuestionResultsEventData
                            {
                                QuestionNumber = 2,
                                ReceivedCountQ1 = 130,
                                ReceivedCountQ2 = 240,
                                ReceivedCountUnspecified = 5,
                            },
                        },
                    },
                },
        };
        PublishAusmittlungBusinessEvent(resultEntered, resultId);

        var correctionEntered = new VoteResultCorrectionEntered
        {
            EventInfo = GetEventInfo(2015),
            VoteResultId = resultId,
            Results =
                {
                    new VoteBallotResultsEventData
                    {
                        BallotId = "6f4f9d65-9265-4b68-9e89-e94e7afbd097",
                        QuestionResults =
                        {
                            new VoteBallotQuestionResultsEventData
                            {
                                QuestionNumber = 1,
                                ReceivedCountNo = 150,
                                ReceivedCountYes = 80,
                                ReceivedCountUnspecified = 13,
                            },
                            new VoteBallotQuestionResultsEventData
                            {
                                QuestionNumber = 2,
                                ReceivedCountNo = 100,
                                ReceivedCountYes = 8,
                                ReceivedCountUnspecified = 8,
                            },
                        },
                        TieBreakQuestionResults =
                        {
                            new VoteTieBreakQuestionResultsEventData
                            {
                                QuestionNumber = 1,
                                ReceivedCountQ1 = 140,
                                ReceivedCountQ2 = 30,
                                ReceivedCountUnspecified = 8,
                            },
                            new VoteTieBreakQuestionResultsEventData
                            {
                                QuestionNumber = 2,
                                ReceivedCountQ1 = 240,
                                ReceivedCountQ2 = 130,
                                ReceivedCountUnspecified = 5,
                            },
                        },
                    },
                },
        };
        PublishAusmittlungBusinessEvent(correctionEntered, resultId);

        SeedVoteBundleEvents();

        var submissionFinished = new VoteResultSubmissionFinished
        {
            EventInfo = GetEventInfo(2223),
            VoteResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(submissionFinished, resultId);

        SeedVoteAfterTestingPhaseEndedEvents();

        var flaggedForCorrection = new VoteResultFlaggedForCorrection
        {
            EventInfo = GetEventInfo(2226, true),
            VoteResultId = resultId,
            Comment = "Bitte korrigieren",
        };
        PublishAusmittlungBusinessEvent(flaggedForCorrection, resultId);

        var correctionFinished = new VoteResultCorrectionFinished
        {
            EventInfo = GetEventInfo(2230),
            VoteResultId = resultId,
            Comment = "Korrigiert",
        };
        PublishAusmittlungBusinessEvent(correctionFinished, resultId);

        var auditedTentatively = new VoteResultAuditedTentatively
        {
            EventInfo = GetEventInfo(2236, true),
            VoteResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(auditedTentatively, resultId);

        var plausibilised = new VoteResultPlausibilised
        {
            EventInfo = GetEventInfo(2340, true),
            VoteResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(plausibilised, resultId);

        var resettedAuditedTentatively = new VoteResultResettedToAuditedTentatively
        {
            EventInfo = GetEventInfo(2443, true),
            VoteResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(resettedAuditedTentatively, resultId);

        var resetted = new VoteResultResettedToSubmissionFinished
        {
            EventInfo = GetEventInfo(2445, true),
            VoteResultId = resultId,
        };
        PublishAusmittlungBusinessEvent(resetted, resultId);
    }

    private void SeedVoteBundleEvents()
    {
        var resultId = "d453bc14-b433-4394-95af-4121ffa8674e";
        var ballotResultId = "4c976f44-ca66-44d7-9017-a3d353a381b9";
        var bundleId = "c4e01944-a47b-48e2-8ada-1f2e3739679e";
        var ballotNumber = 1;

        var bundleCreated = new VoteResultBundleCreated
        {
            EventInfo = GetEventInfo(2100),
            VoteResultId = resultId,
            BallotResultId = ballotResultId,
            BundleId = bundleId,
            BundleNumber = 1,
            ResultEntryParams = new VoteResultEntryParamsEventData
            {
                BallotBundleSampleSizePercent = 20,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            },
        };
        PublishAusmittlungBusinessEvent(bundleCreated, bundleId, Host1, AusmittlungKeyHost1AfterReboot);

        var ballotCreated = new VoteResultBallotCreated
        {
            EventInfo = GetEventInfo(2105),
            BallotResultId = resultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
            QuestionAnswers =
                {
                    new VoteResultBallotUpdatedQuestionAnswerEventData
                    {
                        Answer = SharedProto.BallotQuestionAnswer.Yes,
                        QuestionNumber = 1,
                    },
                },
        };
        PublishAusmittlungBusinessEvent(ballotCreated, bundleId, Host1, AusmittlungKeyHost1AfterReboot);

        var ballotUpdated = new VoteResultBallotUpdated
        {
            EventInfo = GetEventInfo(2108),
            BallotResultId = ballotResultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
            QuestionAnswers =
                {
                    new VoteResultBallotUpdatedQuestionAnswerEventData
                    {
                        Answer = SharedProto.BallotQuestionAnswer.No,
                        QuestionNumber = 1,
                    },
                },
        };
        PublishAusmittlungBusinessEvent(ballotUpdated, bundleId, Host1, AusmittlungKeyHost1AfterReboot);

        var ballotDeleted = new VoteResultBallotDeleted
        {
            EventInfo = GetEventInfo(2112),
            BallotResultId = ballotResultId,
            BundleId = bundleId,
            BallotNumber = ballotNumber,
        };
        PublishAusmittlungBusinessEvent(ballotDeleted, bundleId, Host1, AusmittlungKeyHost1AfterReboot);

        var submissionFinished = new VoteResultBundleSubmissionFinished
        {
            EventInfo = GetEventInfo(2116),
            BallotResultId = ballotResultId,
            BundleId = bundleId,
            SampleBallotNumbers = { 1, 3 },
        };
        PublishAusmittlungBusinessEvent(submissionFinished, bundleId, Host1, AusmittlungKeyHost1AfterReboot);

        var reviewRejected = new VoteResultBundleReviewRejected
        {
            EventInfo = GetEventInfo(2124),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(reviewRejected, bundleId, Host1, AusmittlungKeyHost1AfterReboot);

        var correctionFinished = new VoteResultBundleCorrectionFinished
        {
            EventInfo = GetEventInfo(2128),
            BundleId = bundleId,
            BallotResultId = "c94087d5-4fd7-4ca9-9c36-178bb3b42e5c",
            SampleBallotNumbers = { 1, 3 },
        };
        PublishAusmittlungBusinessEvent(correctionFinished, bundleId, Host1, AusmittlungKeyHost1AfterReboot);

        var reviewSucceeded = new VoteResultBundleReviewSucceeded
        {
            EventInfo = GetEventInfo(2132),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(reviewSucceeded, bundleId, Host1, AusmittlungKeyHost1AfterReboot);

        var bundleDeleted = new VoteResultBundleDeleted
        {
            EventInfo = GetEventInfo(2140),
            BundleId = bundleId,
        };
        PublishAusmittlungBusinessEvent(bundleDeleted, bundleId, Host1, AusmittlungKeyHost1AfterReboot);
    }

    private void SeedMajorityElectionAfterTestingPhaseEndedEvents()
    {
        var electionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund;
        var electionAfterTestingPhaseUpdated = new ProtoBasis.MajorityElectionAfterTestingPhaseUpdated
        {
            Id = electionId,
            ShortDescription = { LanguageUtil.MockAllLanguages("Mw SG UPDATE") },
            PoliticalBusinessNumber = "201 UPDATE",
            OfficialDescription = { LanguageUtil.MockAllLanguages("201 Official UPDATE") },
            EnforceEmptyVoteCountingForCountingCircles = false,
            EnforceResultEntryForCountingCircles = true,
            ReportDomainOfInfluenceLevel = 1,
            EventInfo = GetBasisEventInfo(225),
        };

        PublishBasisBusinessEvent(electionAfterTestingPhaseUpdated, electionId, Host1, BasisKeyHost1AfterTestingPhaseEnded, ContestIdGuid, AggregateNames.MajorityElection);

        var secondaryElectionAfterTestingPhaseUpdated = new ProtoBasis.SecondaryMajorityElectionAfterTestingPhaseUpdated
        {
            Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            ShortDescription = { LanguageUtil.MockAllLanguages("SME ShortDescription UPDATE") },
            PoliticalBusinessNumber = "n1 UPDATE",
            PrimaryMajorityElectionId = electionId,
            OfficialDescription = { LanguageUtil.MockAllLanguages("SME Official Update") },
            EventInfo = GetBasisEventInfo(225),
        };

        PublishBasisBusinessEvent(secondaryElectionAfterTestingPhaseUpdated, electionId, Host1, BasisKeyHost1AfterTestingPhaseEnded, ContestIdGuid, AggregateNames.MajorityElection);
    }

    private void SeedProportionalElectionAfterTestingPhaseEndedEvents()
    {
        var electionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund;
        var electionAfterTestingPhaseUpdated = new ProtoBasis.ProportionalElectionAfterTestingPhaseUpdated
        {
            Id = electionId,
            ShortDescription = { LanguageUtil.MockAllLanguages("Pw SG UPDATE") },
            PoliticalBusinessNumber = "201 UPDATE",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Official UPDATE") },
            EnforceEmptyVoteCountingForCountingCircles = false,
            EventInfo = GetBasisEventInfo(1225),
        };

        PublishBasisBusinessEvent(electionAfterTestingPhaseUpdated, electionId, Host1, BasisKeyHost1AfterTestingPhaseEnded, ContestIdGuid, AggregateNames.ProportionalElection);
    }

    private void SeedVoteAfterTestingPhaseEndedEvents()
    {
        var voteId = VoteMockedData.IdStGallenVoteInContestBund;
        var voteAfterTestingPhaseUpdated = new ProtoBasis.VoteAfterTestingPhaseUpdated
        {
            Id = voteId,
            ShortDescription = { LanguageUtil.MockAllLanguages("Abst SG UPDATE") },
            PoliticalBusinessNumber = "201 UPDATE",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Official UPDATE") },
            InternalDescription = "Internal UPDATE",
            EventInfo = GetBasisEventInfo(2225),
        };

        PublishBasisBusinessEvent(voteAfterTestingPhaseUpdated, voteId, Host1, BasisKeyHost1AfterTestingPhaseEnded, ContestIdGuid, AggregateNames.Vote);
    }

    private void SeedExportEvents()
    {
        var eventData = new ExportGenerated
        {
            EventInfo = GetEventInfo(30),
            Key = AusmittlungPdfProportionalElectionTemplates.ListCandidateVoteSourcesEndResults.Key,
            ContestId = ContestId,
            RequestId = "9d74f9c8-90bc-44d2-932a-d67b2046f2d8",
        };
        PublishAusmittlungBusinessEvent(eventData, Guid.NewGuid().ToString(), Host1, AusmittlungKeyHost1);
    }
}
