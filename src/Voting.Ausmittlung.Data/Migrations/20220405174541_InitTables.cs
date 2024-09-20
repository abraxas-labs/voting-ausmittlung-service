// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class InitTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CantonSettings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Canton = table.Column<int>(type: "integer", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                AuthorityName = table.Column<string>(type: "text", nullable: false),
                ProportionalElectionMandateAlgorithms = table.Column<List<int>>(type: "integer[]", nullable: false),
                MajorityElectionAbsoluteMajorityAlgorithm = table.Column<int>(type: "integer", nullable: false),
                MajorityElectionInvalidVotes = table.Column<bool>(type: "boolean", nullable: false),
                SwissAbroadVotingRight = table.Column<int>(type: "integer", nullable: false),
                SwissAbroadVotingRightDomainOfInfluenceTypes = table.Column<List<int>>(type: "integer[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CantonSettings", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EventProcessingStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PreparePosition = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                CommitPosition = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                EventNumber = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventProcessingStates", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CantonSettingsVotingCardChannels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VotingChannel = table.Column<int>(type: "integer", nullable: false),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                CantonSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CantonSettingsVotingCardChannels", x => x.Id);
                table.ForeignKey(
                    name: "FK_CantonSettingsVotingCardChannels_CantonSettings_CantonSetti~",
                    column: x => x.CantonSettingsId,
                    principalTable: "CantonSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Authorities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Phone = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
                Street = table.Column<string>(type: "text", nullable: false),
                Zip = table.Column<string>(type: "text", nullable: false),
                City = table.Column<string>(type: "text", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Authorities", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BallotQuestions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotQuestions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BallotQuestionTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                Question = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotQuestionTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_BallotQuestionTranslations_BallotQuestions_BallotQuestionId",
                    column: x => x.BallotQuestionId,
                    principalTable: "BallotQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BallotQuestionEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                BallotEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerYes = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerNo = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerUnspecified = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfAnswerYes = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfAnswerNo = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfAnswerUnspecified = table.Column<int>(type: "integer", nullable: false),
                CountOfCountingCircleYes = table.Column<int>(type: "integer", nullable: false),
                CountOfCountingCircleNo = table.Column<int>(type: "integer", nullable: false),
                HasCountingCircleMajority = table.Column<bool>(type: "boolean", nullable: false),
                HasCountingCircleUnanimity = table.Column<bool>(type: "boolean", nullable: false),
                Accepted = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotQuestionEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_BallotQuestionEndResults_BallotQuestions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "BallotQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BallotQuestionResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                BallotResultId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerYes = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerNo = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerUnspecified = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfAnswerYes = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_TotalCountOfAnswerNo = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_TotalCountOfAnswerUnspecified = table.Column<int>(type: "integer", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotQuestionResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_BallotQuestionResults_BallotQuestions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "BallotQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Ballots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                BallotType = table.Column<int>(type: "integer", nullable: false),
                HasTieBreakQuestions = table.Column<bool>(type: "boolean", nullable: false),
                VoteId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ballots", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BallotTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_BallotTranslations_Ballots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "Ballots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TieBreakQuestions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                Question1Number = table.Column<int>(type: "integer", nullable: false),
                Question2Number = table.Column<int>(type: "integer", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TieBreakQuestions", x => x.Id);
                table.ForeignKey(
                    name: "FK_TieBreakQuestions_Ballots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "Ballots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TieBreakQuestionTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TieBreakQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                Question = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TieBreakQuestionTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_TieBreakQuestionTranslations_TieBreakQuestions_TieBreakQues~",
                    column: x => x.TieBreakQuestionId,
                    principalTable: "TieBreakQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BallotEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                VoteEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfVoters_VoterParticipation = table.Column<decimal>(type: "numeric", nullable: false),
                CountOfVoters_EVotingReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalInvalidBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalBlankBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalUnaccountedBallots = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_BallotEndResults_Ballots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "Ballots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TieBreakQuestionEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                BallotEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerQ1 = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerQ2 = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerUnspecified = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfAnswerQ1 = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfAnswerQ2 = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfAnswerUnspecified = table.Column<int>(type: "integer", nullable: false),
                CountOfCountingCircleQ1 = table.Column<int>(type: "integer", nullable: false),
                CountOfCountingCircleQ2 = table.Column<int>(type: "integer", nullable: false),
                HasCountingCircleQ1Majority = table.Column<bool>(type: "boolean", nullable: false),
                HasCountingCircleQ2Majority = table.Column<bool>(type: "boolean", nullable: false),
                Q1Accepted = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TieBreakQuestionEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_TieBreakQuestionEndResults_BallotEndResults_BallotEndResult~",
                    column: x => x.BallotEndResultId,
                    principalTable: "BallotEndResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TieBreakQuestionEndResults_TieBreakQuestions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "TieBreakQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BallotResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                VoteResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfVoters_VoterParticipation = table.Column<decimal>(type: "numeric", nullable: false),
                CountOfVoters_EVotingReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalReceivedBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalInvalidBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalBlankBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalAccountedBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_TotalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalUnaccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfBundlesNotReviewedOrDeleted = table.Column<int>(type: "integer", nullable: false),
                ConventionalCountOfDetailedEnteredBallots = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_BallotResults_Ballots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "Ballots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TieBreakQuestionResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                BallotResultId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerQ1 = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerQ2 = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfAnswerUnspecified = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfAnswerQ1 = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_TotalCountOfAnswerQ2 = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_TotalCountOfAnswerUnspecified = table.Column<int>(type: "integer", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TieBreakQuestionResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_TieBreakQuestionResults_BallotResults_BallotResultId",
                    column: x => x.BallotResultId,
                    principalTable: "BallotResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TieBreakQuestionResults_TieBreakQuestions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "TieBreakQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteResultBundles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotResultId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                CreatedBy_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                CreatedBy_FirstName = table.Column<string>(type: "text", nullable: false),
                CreatedBy_LastName = table.Column<string>(type: "text", nullable: false),
                ReviewedBy_SecureConnectId = table.Column<string>(type: "text", nullable: true),
                ReviewedBy_FirstName = table.Column<string>(type: "text", nullable: true),
                ReviewedBy_LastName = table.Column<string>(type: "text", nullable: true),
                CountOfBallots = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteResultBundles", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteResultBundles_BallotResults_BallotResultId",
                    column: x => x.BallotResultId,
                    principalTable: "BallotResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteResultBallots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                MarkedForReview = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteResultBallots", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteResultBallots_VoteResultBundles_BundleId",
                    column: x => x.BundleId,
                    principalTable: "VoteResultBundles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteResultBallotQuestionAnswers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                Answer = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteResultBallotQuestionAnswers", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteResultBallotQuestionAnswers_BallotQuestions_QuestionId",
                    column: x => x.QuestionId,
                    principalTable: "BallotQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_VoteResultBallotQuestionAnswers_VoteResultBallots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "VoteResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteResultBallotTieBreakQuestionAnswers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                Answer = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteResultBallotTieBreakQuestionAnswers", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteResultBallotTieBreakQuestionAnswers_TieBreakQuestions_Q~",
                    column: x => x.QuestionId,
                    principalTable: "TieBreakQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_VoteResultBallotTieBreakQuestionAnswers_VoteResultBallots_B~",
                    column: x => x.BallotId,
                    principalTable: "VoteResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ComparisonCountOfVotersConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                ThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
                PlausibilisationConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComparisonCountOfVotersConfigurations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ComparisonVoterParticipationConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MainLevel = table.Column<int>(type: "integer", nullable: false),
                ComparisonLevel = table.Column<int>(type: "integer", nullable: false),
                ThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
                PlausibilisationConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComparisonVoterParticipationConfigurations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ComparisonVotingChannelConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VotingChannel = table.Column<int>(type: "integer", nullable: false),
                ThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
                PlausibilisationConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComparisonVotingChannelConfigurations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ContestCountOfVotersInformationSubTotals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters = table.Column<int>(type: "integer", nullable: false),
                VoterType = table.Column<int>(type: "integer", nullable: false),
                ContestDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestCountOfVotersInformationSubTotals", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ContestDetails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestDetails", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ContestVotingCardResultDetails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfReceivedVotingCards = table.Column<int>(type: "integer", nullable: false),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                Channel = table.Column<int>(type: "integer", nullable: false),
                DomainOfInfluenceType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestVotingCardResultDetails", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestVotingCardResultDetails_ContestDetails_ContestDetail~",
                    column: x => x.ContestDetailsId,
                    principalTable: "ContestDetails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Contests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateTime>(type: "date", nullable: false),
                EndOfTestingPhase = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingResultsImported = table.Column<bool>(type: "boolean", nullable: false),
                EVoting = table.Column<bool>(type: "boolean", nullable: false),
                EVotingFrom = table.Column<DateTime>(type: "date", nullable: true),
                EVotingTo = table.Column<DateTime>(type: "date", nullable: true),
                PreviousContestId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Contests", x => x.Id);
                table.ForeignKey(
                    name: "FK_Contests_Contests_PreviousContestId",
                    column: x => x.PreviousContestId,
                    principalTable: "Contests",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "ContestTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestTranslations_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Bfs = table.Column<string>(type: "text", nullable: false),
                Code = table.Column<string>(type: "text", nullable: false),
                ContactPersonSameDuringEventAsAfter = table.Column<bool>(type: "boolean", nullable: false),
                BasisCountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                SnapshotContestId = table.Column<Guid>(type: "uuid", nullable: true),
                ContestCountingCircleContactPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                MustUpdateContactPersons = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircles", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountingCircles_Contests_SnapshotContestId",
                    column: x => x.SnapshotContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluencePermissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<string>(type: "text", nullable: false),
                BasisDomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                BasisCountingCircleIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                CountingCircleIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                IsParent = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluencePermissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluencePermissions_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluences",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                ShortName = table.Column<string>(type: "text", nullable: false),
                Bfs = table.Column<string>(type: "text", nullable: false),
                Code = table.Column<string>(type: "text", nullable: false),
                SortNumber = table.Column<int>(type: "integer", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                AuthorityName = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Canton = table.Column<int>(type: "integer", nullable: false),
                ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                BasisDomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                SnapshotContestId = table.Column<Guid>(type: "uuid", nullable: true),
                ContactPerson_FirstName = table.Column<string>(type: "text", nullable: false),
                ContactPerson_FamilyName = table.Column<string>(type: "text", nullable: false),
                ContactPerson_Phone = table.Column<string>(type: "text", nullable: false),
                ContactPerson_MobilePhone = table.Column<string>(type: "text", nullable: false),
                ContactPerson_Email = table.Column<string>(type: "text", nullable: false),
                CantonDefaults_ProportionalElectionMandateAlgorithms = table.Column<List<int>>(type: "integer[]", nullable: false),
                CantonDefaults_MajorityElectionAbsoluteMajorityAlgorithm = table.Column<int>(type: "integer", nullable: false),
                CantonDefaults_MajorityElectionInvalidVotes = table.Column<bool>(type: "boolean", nullable: false),
                CantonDefaults_SwissAbroadVotingRight = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluences", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluences_Contests_SnapshotContestId",
                    column: x => x.SnapshotContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DomainOfInfluences_DomainOfInfluences_ParentId",
                    column: x => x.ParentId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionUnions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionUnions", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionUnions_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                SecureConnectId = table.Column<string>(type: "text", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnions_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ResultImports",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                Started = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Completed = table.Column<bool>(type: "boolean", nullable: false),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
                StartedBy_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                StartedBy_FirstName = table.Column<string>(type: "text", nullable: false),
                StartedBy_LastName = table.Column<string>(type: "text", nullable: false),
                FileName = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResultImports", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResultImports_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ContestCountingCircleDetails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                EVoting = table.Column<bool>(type: "boolean", nullable: false),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestCountingCircleDetails", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestCountingCircleDetails_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ContestCountingCircleDetails_CountingCircles_CountingCircle~",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircleContactPersons",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                FamilyName = table.Column<string>(type: "text", nullable: false),
                Phone = table.Column<string>(type: "text", nullable: false),
                MobilePhone = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
                CountingCircleDuringEventId = table.Column<Guid>(type: "uuid", nullable: true),
                CountingCircleAfterEventId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircleContactPersons", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountingCircleContactPersons_CountingCircles_CountingCircl~1",
                    column: x => x.CountingCircleDuringEventId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CountingCircleContactPersons_CountingCircles_CountingCircle~",
                    column: x => x.CountingCircleAfterEventId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceCantonDefaultsVotingCardChannels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VotingChannel = table.Column<int>(type: "integer", nullable: false),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                DomainOfInfluenceCantonDefaultsDomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceCantonDefaultsVotingCardChannels", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceCantonDefaultsVotingCardChannels_DomainOfI~",
                    column: x => x.DomainOfInfluenceCantonDefaultsDomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceCountingCircles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                ComparisonCountOfVotersCategory = table.Column<int>(type: "integer", nullable: false),
                Inherited = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceCountingCircles", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceCountingCircles_CountingCircles_CountingCi~",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceCountingCircles_DomainOfInfluences_DomainO~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceParties",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BaseDomainOfInfluencePartyId = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
                SnapshotContestId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceParties", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceParties_Contests_SnapshotContestId",
                    column: x => x.SnapshotContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceParties_DomainOfInfluences_DomainOfInfluen~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ExportConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                EaiMessageType = table.Column<string>(type: "text", nullable: false),
                ExportKeys = table.Column<string[]>(type: "text[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExportConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExportConfigurations_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MandateAlgorithm = table.Column<int>(type: "integer", nullable: false),
                IndividualEmptyBallotsAllowed = table.Column<bool>(type: "boolean", nullable: false),
                CandidateCheckDigit = table.Column<bool>(type: "boolean", nullable: false),
                InvalidVotes = table.Column<bool>(type: "boolean", nullable: false),
                BallotBundleSize = table.Column<int>(type: "integer", nullable: false),
                BallotBundleSampleSize = table.Column<int>(type: "integer", nullable: false),
                AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: false),
                BallotNumberGeneration = table.Column<int>(type: "integer", nullable: false),
                AutomaticEmptyVoteCounting = table.Column<bool>(type: "boolean", nullable: false),
                EnforceEmptyVoteCountingForCountingCircles = table.Column<bool>(type: "boolean", nullable: false),
                ResultEntry = table.Column<int>(type: "integer", nullable: false),
                EnforceResultEntryForCountingCircles = table.Column<bool>(type: "boolean", nullable: false),
                ReportDomainOfInfluenceLevel = table.Column<int>(type: "integer", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElections", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElections_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElections_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PlausibilisationConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = table.Column<decimal>(type: "numeric", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlausibilisationConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlausibilisationConfigurations_DomainOfInfluences_DomainOfI~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MandateAlgorithm = table.Column<int>(type: "integer", nullable: false),
                CandidateCheckDigit = table.Column<bool>(type: "boolean", nullable: false),
                BallotBundleSize = table.Column<int>(type: "integer", nullable: false),
                BallotBundleSampleSize = table.Column<int>(type: "integer", nullable: false),
                AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: false),
                BallotNumberGeneration = table.Column<int>(type: "integer", nullable: false),
                AutomaticEmptyVoteCounting = table.Column<bool>(type: "boolean", nullable: false),
                EnforceEmptyVoteCountingForCountingCircles = table.Column<bool>(type: "boolean", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElections", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElections_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElections_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ResultExportConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExportConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                IntervalMinutes = table.Column<int>(type: "integer", nullable: true),
                NextExecution = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                EaiMessageType = table.Column<string>(type: "text", nullable: false),
                ExportKeys = table.Column<string[]>(type: "text[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResultExportConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResultExportConfigurations_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ResultExportConfigurations_DomainOfInfluences_DomainOfInflu~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SimplePoliticalBusinesses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessType = table.Column<int>(type: "integer", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: true),
                CountOfSecondaryBusinesses = table.Column<int>(type: "integer", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SimplePoliticalBusinesses", x => x.Id);
                table.ForeignKey(
                    name: "FK_SimplePoliticalBusinesses_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SimplePoliticalBusinesses_DomainOfInfluences_DomainOfInflue~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Votes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReportDomainOfInfluenceLevel = table.Column<int>(type: "integer", nullable: false),
                ResultAlgorithm = table.Column<int>(type: "integer", nullable: false),
                BallotBundleSampleSizePercent = table.Column<int>(type: "integer", nullable: false),
                AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: false),
                EnforceResultEntryForCountingCircles = table.Column<bool>(type: "boolean", nullable: false),
                ResultEntry = table.Column<int>(type: "integer", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Votes", x => x.Id);
                table.ForeignKey(
                    name: "FK_Votes_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Votes_DomainOfInfluences_DomainOfInfluenceId",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnionLists",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderNumber = table.Column<string>(type: "text", nullable: false),
                ProportionalElectionUnionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnionLists", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionLists_ProportionalElectionUnions_P~",
                    column: x => x.ProportionalElectionUnionId,
                    principalTable: "ProportionalElectionUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountOfVotersInformationSubTotals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters = table.Column<int>(type: "integer", nullable: true),
                VoterType = table.Column<int>(type: "integer", nullable: false),
                ContestCountingCircleDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountOfVotersInformationSubTotals", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountOfVotersInformationSubTotals_ContestCountingCircleDeta~",
                    column: x => x.ContestCountingCircleDetailsId,
                    principalTable: "ContestCountingCircleDetails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VotingCardResultDetails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestCountingCircleDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfReceivedVotingCards = table.Column<int>(type: "integer", nullable: true),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                Channel = table.Column<int>(type: "integer", nullable: false),
                DomainOfInfluenceType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VotingCardResultDetails", x => x.Id);
                table.ForeignKey(
                    name: "FK_VotingCardResultDetails_ContestCountingCircleDetails_Contes~",
                    column: x => x.ContestCountingCircleDetailsId,
                    principalTable: "ContestCountingCircleDetails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluencePartyTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluencePartyId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluencePartyTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluencePartyTranslations_DomainOfInfluenceParties~",
                    column: x => x.DomainOfInfluencePartyId,
                    principalTable: "DomainOfInfluenceParties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ElectionGroups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                PrimaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfSecondaryElections = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ElectionGroups", x => x.Id);
                table.ForeignKey(
                    name: "FK_ElectionGroups_MajorityElections_PrimaryMajorityElectionId",
                    column: x => x.PrimaryMajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionBallotGroups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                AllCandidateCountsOk = table.Column<bool>(type: "boolean", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionBallotGroups", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroups_MajorityElections_MajorityElec~",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                PoliticalFirstName = table.Column<string>(type: "text", nullable: false),
                PoliticalLastName = table.Column<string>(type: "text", nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "date", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Incumbent = table.Column<bool>(type: "boolean", nullable: false),
                ZipCode = table.Column<string>(type: "text", nullable: false),
                Locality = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionCandidates_MajorityElections_MajorityElecti~",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfVoters_VoterParticipation = table.Column<decimal>(type: "numeric", nullable: false),
                CountOfVoters_EVotingReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalInvalidBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalBlankBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalUnaccountedBallots = table.Column<int>(type: "integer", nullable: false),
                Calculation_AbsoluteMajority = table.Column<int>(type: "integer", nullable: true),
                EVotingSubTotal_IndividualVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_IndividualVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
                CountOfDoneCountingCircles = table.Column<int>(type: "integer", nullable: false),
                TotalCountOfCountingCircles = table.Column<int>(type: "integer", nullable: false),
                Finalized = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionEndResults_MajorityElections_MajorityElecti~",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                Entry = table.Column<int>(type: "integer", nullable: false),
                EntryParams_BallotBundleSize = table.Column<int>(type: "integer", nullable: true),
                EntryParams_BallotBundleSampleSize = table.Column<int>(type: "integer", nullable: true),
                EntryParams_AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: true),
                EntryParams_BallotNumberGeneration = table.Column<int>(type: "integer", nullable: true),
                EntryParams_AutomaticEmptyVoteCounting = table.Column<bool>(type: "boolean", nullable: true),
                EVotingSubTotal_IndividualVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_IndividualVoteCount = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_EmptyVoteCount = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_InvalidVoteCount = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_TotalCandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
                ConventionalCountOfBallotGroupVotes = table.Column<int>(type: "integer", nullable: false),
                ConventionalCountOfDetailedEnteredBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfElectionsWithUnmappedWriteIns = table.Column<int>(type: "integer", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                SubmissionDoneTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AuditedTentativelyTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_VoterParticipation = table.Column<decimal>(type: "numeric", nullable: false),
                CountOfVoters_EVotingReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalReceivedBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalInvalidBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalBlankBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalAccountedBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_TotalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalUnaccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfBundlesNotReviewedOrDeleted = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionResults_CountingCircles_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionResults_MajorityElections_MajorityElectionId",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionTranslations_MajorityElections_MajorityElec~",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionUnionEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionUnionId = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionUnionEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionUnionEntries_MajorityElections_MajorityElec~",
                    column: x => x.MajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionUnionEntries_MajorityElectionUnions_Majorit~",
                    column: x => x.MajorityElectionUnionId,
                    principalTable: "MajorityElectionUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionEndResult",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfVoters_VoterParticipation = table.Column<decimal>(type: "numeric", nullable: false),
                CountOfVoters_EVotingReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalInvalidBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalBlankBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalUnaccountedBallots = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfUnmodifiedLists = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfModifiedLists = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfListsWithoutParty = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfBlankRowsOnListsWithoutParty = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfUnmodifiedLists = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfModifiedLists = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfListsWithoutParty = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfBlankRowsOnListsWithoutParty = table.Column<int>(type: "integer", nullable: false),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
                CountOfDoneCountingCircles = table.Column<int>(type: "integer", nullable: false),
                TotalCountOfCountingCircles = table.Column<int>(type: "integer", nullable: false),
                Finalized = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionEndResult", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionEndResult_ProportionalElections_Proport~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionLists",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrderNumber = table.Column<string>(type: "text", nullable: false),
                BlankRowCount = table.Column<int>(type: "integer", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionLists", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionLists_ProportionalElections_Proportiona~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                EntryParams_BallotBundleSize = table.Column<int>(type: "integer", nullable: false),
                EntryParams_BallotBundleSampleSize = table.Column<int>(type: "integer", nullable: false),
                EntryParams_AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: false),
                EntryParams_BallotNumberGeneration = table.Column<int>(type: "integer", nullable: false),
                EntryParams_AutomaticEmptyVoteCounting = table.Column<bool>(type: "boolean", nullable: false),
                EVotingSubTotal_TotalCountOfUnmodifiedLists = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfModifiedLists = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfListsWithoutParty = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCountOfBlankRowsOnListsWithoutParty = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfUnmodifiedLists = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfModifiedLists = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfListsWithoutParty = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCountOfBlankRowsOnListsWithoutParty = table.Column<int>(type: "integer", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                SubmissionDoneTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AuditedTentativelyTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_VoterParticipation = table.Column<decimal>(type: "numeric", nullable: false),
                CountOfVoters_EVotingReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_ConventionalReceivedBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalInvalidBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalBlankBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_ConventionalAccountedBallots = table.Column<int>(type: "integer", nullable: true),
                CountOfVoters_TotalReceivedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalAccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters_TotalUnaccountedBallots = table.Column<int>(type: "integer", nullable: false),
                CountOfBundlesNotReviewedOrDeleted = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionResults_CountingCircles_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionResults_ProportionalElections_Proportio~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionTranslations_ProportionalElections_Prop~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnionEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionUnionId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnionEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionEntries_ProportionalElections_Prop~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionEntries_ProportionalElectionUnions~",
                    column: x => x.ProportionalElectionUnionId,
                    principalTable: "ProportionalElectionUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ResultExportConfigurationPoliticalBusinesses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                ResultExportConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResultExportConfigurationPoliticalBusinesses", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResultExportConfigurationPoliticalBusinesses_ResultExportCo~",
                    column: x => x.ResultExportConfigurationId,
                    principalTable: "ResultExportConfigurations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ResultExportConfigurationPoliticalBusinesses_SimplePolitica~",
                    column: x => x.PoliticalBusinessId,
                    principalTable: "SimplePoliticalBusinesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SimpleCountingCircleResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                SubmissionDoneTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                HasComments = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SimpleCountingCircleResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_SimpleCountingCircleResults_CountingCircles_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SimpleCountingCircleResults_SimplePoliticalBusinesses_Polit~",
                    column: x => x.PoliticalBusinessId,
                    principalTable: "SimplePoliticalBusinesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SimplePoliticalBusinessTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SimplePoliticalBusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SimplePoliticalBusinessTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_SimplePoliticalBusinessTranslations_SimplePoliticalBusiness~",
                    column: x => x.SimplePoliticalBusinessId,
                    principalTable: "SimplePoliticalBusinesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VoteId = table.Column<Guid>(type: "uuid", nullable: false),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
                CountOfDoneCountingCircles = table.Column<int>(type: "integer", nullable: false),
                TotalCountOfCountingCircles = table.Column<int>(type: "integer", nullable: false),
                Finalized = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteEndResults_Votes_VoteId",
                    column: x => x.VoteId,
                    principalTable: "Votes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VoteId = table.Column<Guid>(type: "uuid", nullable: false),
                Entry = table.Column<int>(type: "integer", nullable: false),
                EntryParams_BallotBundleSampleSizePercent = table.Column<int>(type: "integer", nullable: true),
                EntryParams_AutomaticBallotBundleNumberGeneration = table.Column<bool>(type: "boolean", nullable: true),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                SubmissionDoneTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AuditedTentativelyTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteResults_CountingCircles_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_VoteResults_Votes_VoteId",
                    column: x => x.VoteId,
                    principalTable: "Votes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VoteId = table.Column<Guid>(type: "uuid", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteTranslations_Votes_VoteId",
                    column: x => x.VoteId,
                    principalTable: "Votes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnionListTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                ProportionalElectionUnionListId = table.Column<Guid>(type: "uuid", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnionListTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionListTranslations_ProportionalElect~",
                    column: x => x.ProportionalElectionUnionListId,
                    principalTable: "ProportionalElectionUnionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AllowedCandidates = table.Column<int>(type: "integer", nullable: false),
                PrimaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                ElectionGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessNumber = table.Column<string>(type: "text", nullable: false),
                InternalDescription = table.Column<string>(type: "text", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElections", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElections_ElectionGroups_ElectionGroupId",
                    column: x => x.ElectionGroupId,
                    principalTable: "ElectionGroups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElections_MajorityElections_PrimaryMajorit~",
                    column: x => x.PrimaryMajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionCandidateTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                Occupation = table.Column<string>(type: "text", nullable: false),
                OccupationTitle = table.Column<string>(type: "text", nullable: false),
                Party = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionCandidateTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionCandidateTranslations_MajorityElectionCandi~",
                    column: x => x.MajorityElectionCandidateId,
                    principalTable: "MajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionCandidateEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                Rank = table.Column<int>(type: "integer", nullable: false),
                LotDecision = table.Column<bool>(type: "boolean", nullable: false),
                LotDecisionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                LotDecisionRequired = table.Column<bool>(type: "boolean", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                ConventionalVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingVoteCount = table.Column<int>(type: "integer", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionCandidateEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionCandidateEndResults_MajorityElectionCandida~",
                    column: x => x.CandidateId,
                    principalTable: "MajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionCandidateEndResults_MajorityElectionEndResu~",
                    column: x => x.MajorityElectionEndResultId,
                    principalTable: "MajorityElectionEndResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionBallotGroupResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ElectionResultId = table.Column<Guid>(type: "uuid", nullable: false),
                BallotGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionBallotGroupResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupResults_MajorityElectionBallotGr~",
                    column: x => x.BallotGroupId,
                    principalTable: "MajorityElectionBallotGroups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupResults_MajorityElectionResults_~",
                    column: x => x.ElectionResultId,
                    principalTable: "MajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionCandidateResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ElectionResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                ConventionalVoteCount = table.Column<int>(type: "integer", nullable: true),
                EVotingVoteCount = table.Column<int>(type: "integer", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionCandidateResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionCandidateResults_MajorityElectionCandidates~",
                    column: x => x.CandidateId,
                    principalTable: "MajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionCandidateResults_MajorityElectionResults_El~",
                    column: x => x.ElectionResultId,
                    principalTable: "MajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionResultBundles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                CreatedBy_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                CreatedBy_FirstName = table.Column<string>(type: "text", nullable: false),
                CreatedBy_LastName = table.Column<string>(type: "text", nullable: false),
                ReviewedBy_SecureConnectId = table.Column<string>(type: "text", nullable: true),
                ReviewedBy_FirstName = table.Column<string>(type: "text", nullable: true),
                ReviewedBy_LastName = table.Column<string>(type: "text", nullable: true),
                CountOfBallots = table.Column<int>(type: "integer", nullable: false),
                ElectionResultId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionResultBundles", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionResultBundles_MajorityElectionResults_Elect~",
                    column: x => x.ElectionResultId,
                    principalTable: "MajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Accumulated = table.Column<bool>(type: "boolean", nullable: false),
                AccumulatedPosition = table.Column<int>(type: "integer", nullable: false),
                ProportionalElectionListId = table.Column<Guid>(type: "uuid", nullable: false),
                PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                Number = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                PoliticalFirstName = table.Column<string>(type: "text", nullable: false),
                PoliticalLastName = table.Column<string>(type: "text", nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "date", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Incumbent = table.Column<bool>(type: "boolean", nullable: false),
                ZipCode = table.Column<string>(type: "text", nullable: false),
                Locality = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidates_DomainOfInfluenceParties_Par~",
                    column: x => x.PartyId,
                    principalTable: "DomainOfInfluenceParties",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidates_ProportionalElectionLists_Pr~",
                    column: x => x.ProportionalElectionListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionListEndResult",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ListId = table.Column<Guid>(type: "uuid", nullable: false),
                ElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                HasOpenRequiredLotDecisions = table.Column<bool>(type: "boolean", nullable: false),
                EVotingSubTotal_UnmodifiedListsCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_UnmodifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_UnmodifiedListBlankRowsCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ModifiedListsCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ModifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ListVotesCountOnOtherLists = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ModifiedListBlankRowsCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_UnmodifiedListsCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_UnmodifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_UnmodifiedListBlankRowsCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ModifiedListsCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ModifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ListVotesCountOnOtherLists = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ModifiedListBlankRowsCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionListEndResult", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListEndResult_ProportionalElectionEndRe~",
                    column: x => x.ElectionEndResultId,
                    principalTable: "ProportionalElectionEndResult",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListEndResult_ProportionalElectionLists~",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionListTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionListId = table.Column<Guid>(type: "uuid", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionListTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListTranslations_ProportionalElectionLi~",
                    column: x => x.ProportionalElectionListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionListUnions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionRootListUnionId = table.Column<Guid>(type: "uuid", nullable: true),
                ProportionalElectionMainListId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionListUnions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnions_ProportionalElectionLists_Pr~",
                    column: x => x.ProportionalElectionMainListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnions_ProportionalElectionListUnio~",
                    column: x => x.ProportionalElectionRootListUnionId,
                    principalTable: "ProportionalElectionListUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnions_ProportionalElections_Propor~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnionListEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionUnionListId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionListId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnionListEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionListEntries_ProportionalElectionLi~",
                    column: x => x.ProportionalElectionListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionListEntries_ProportionalElectionUn~",
                    column: x => x.ProportionalElectionUnionListId,
                    principalTable: "ProportionalElectionUnionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionBundles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ListId = table.Column<Guid>(type: "uuid", nullable: true),
                Number = table.Column<int>(type: "integer", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                CreatedBy_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                CreatedBy_FirstName = table.Column<string>(type: "text", nullable: false),
                CreatedBy_LastName = table.Column<string>(type: "text", nullable: false),
                ReviewedBy_SecureConnectId = table.Column<string>(type: "text", nullable: true),
                ReviewedBy_FirstName = table.Column<string>(type: "text", nullable: true),
                ReviewedBy_LastName = table.Column<string>(type: "text", nullable: true),
                CountOfBallots = table.Column<int>(type: "integer", nullable: false),
                ElectionResultId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionBundles", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionBundles_ProportionalElectionLists_ListId",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionBundles_ProportionalElectionResults_Ele~",
                    column: x => x.ElectionResultId,
                    principalTable: "ProportionalElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionListResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                ListId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingSubTotal_UnmodifiedListsCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_UnmodifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_UnmodifiedListBlankRowsCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ModifiedListsCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ModifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ListVotesCountOnOtherLists = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ModifiedListBlankRowsCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_UnmodifiedListsCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_UnmodifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_UnmodifiedListBlankRowsCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ModifiedListsCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ModifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ListVotesCountOnOtherLists = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ModifiedListBlankRowsCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionListResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListResults_ProportionalElectionLists_L~",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListResults_ProportionalElectionResults~",
                    column: x => x.ResultId,
                    principalTable: "ProportionalElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnmodifiedListResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                ListId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalVoteCount = table.Column<int>(type: "integer", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnmodifiedListResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnmodifiedListResults_ProportionalElec~1",
                    column: x => x.ResultId,
                    principalTable: "ProportionalElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnmodifiedListResults_ProportionalElect~",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircleResultComments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedBy_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                CreatedBy_FirstName = table.Column<string>(type: "text", nullable: false),
                CreatedBy_LastName = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                CreatedByMonitoringAuthority = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircleResultComments", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountingCircleResultComments_SimpleCountingCircleResults_Re~",
                    column: x => x.ResultId,
                    principalTable: "SimpleCountingCircleResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionBallotGroupEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BlankRowCount = table.Column<int>(type: "integer", nullable: false),
                BallotGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                PrimaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: true),
                SecondaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: true),
                IndividualCandidatesVoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionBallotGroupEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntries_MajorityElectionBallotGr~",
                    column: x => x.BallotGroupId,
                    principalTable: "MajorityElectionBallotGroups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntries_MajorityElections_Primar~",
                    column: x => x.PrimaryMajorityElectionId,
                    principalTable: "MajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntries_SecondaryMajorityElectio~",
                    column: x => x.SecondaryMajorityElectionId,
                    principalTable: "SecondaryMajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecondaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                Number = table.Column<string>(type: "text", nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                PoliticalFirstName = table.Column<string>(type: "text", nullable: false),
                PoliticalLastName = table.Column<string>(type: "text", nullable: false),
                DateOfBirth = table.Column<DateTime>(type: "date", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Incumbent = table.Column<bool>(type: "boolean", nullable: false),
                ZipCode = table.Column<string>(type: "text", nullable: false),
                Locality = table.Column<string>(type: "text", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidates_MajorityElectionCandida~",
                    column: x => x.CandidateReferenceId,
                    principalTable: "MajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidates_SecondaryMajorityElecti~",
                    column: x => x.SecondaryMajorityElectionId,
                    principalTable: "SecondaryMajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecondaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                PrimaryMajorityElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingSubTotal_IndividualVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_IndividualVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_TotalCandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElections_PrimaryMajorityElectionEndResultId",
                    column: x => x.PrimaryMajorityElectionEndResultId,
                    principalTable: "MajorityElectionEndResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecMajElEndResults_SecondaryMajorityElectionId",
                    column: x => x.SecondaryMajorityElectionId,
                    principalTable: "SecondaryMajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PrimaryResultId = table.Column<Guid>(type: "uuid", nullable: false),
                SecondaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingSubTotal_IndividualVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_TotalCandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_IndividualVoteCount = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_EmptyVoteCount = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_InvalidVoteCount = table.Column<int>(type: "integer", nullable: true),
                ConventionalSubTotal_TotalCandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionResults_MajorityElectionResults_Pr~",
                    column: x => x.PrimaryResultId,
                    principalTable: "MajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionResults_SecondaryMajorityElections~",
                    column: x => x.SecondaryMajorityElectionId,
                    principalTable: "SecondaryMajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecondaryMajorityElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionTranslations_SecondaryMajorityElec~",
                    column: x => x.SecondaryMajorityElectionId,
                    principalTable: "SecondaryMajorityElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionWriteInMappings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateResultId = table.Column<Guid>(type: "uuid", nullable: true),
                WriteInCandidateName = table.Column<string>(type: "text", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
                Target = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionWriteInMappings", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionWriteInMappings_MajorityElectionCandidateRe~",
                    column: x => x.CandidateResultId,
                    principalTable: "MajorityElectionCandidateResults",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_MajorityElectionWriteInMappings_MajorityElectionResults_Res~",
                    column: x => x.ResultId,
                    principalTable: "MajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionResultBallots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                IndividualVoteCount = table.Column<int>(type: "integer", nullable: false),
                InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
                CandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                MarkedForReview = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionResultBallots", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionResultBallots_MajorityElectionResultBundles~",
                    column: x => x.BundleId,
                    principalTable: "MajorityElectionResultBundles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionCandidateTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                Occupation = table.Column<string>(type: "text", nullable: false),
                OccupationTitle = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionCandidateTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateTranslations_ProportionalElect~",
                    column: x => x.ProportionalElectionCandidateId,
                    principalTable: "ProportionalElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionCandidateEndResult",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ListEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_UnmodifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ModifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_CountOfVotesOnOtherLists = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_CountOfVotesFromAccumulations = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_UnmodifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ModifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_CountOfVotesOnOtherLists = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_CountOfVotesFromAccumulations = table.Column<int>(type: "integer", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                Rank = table.Column<int>(type: "integer", nullable: false),
                LotDecision = table.Column<bool>(type: "boolean", nullable: false),
                LotDecisionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                LotDecisionRequired = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionCandidateEndResult", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateEndResults_CandidateId",
                    column: x => x.CandidateId,
                    principalTable: "ProportionalElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateEndResults_ListEndResultId",
                    column: x => x.ListEndResultId,
                    principalTable: "ProportionalElectionListEndResult",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "HagenbachBischoffGroup",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EndResultId = table.Column<Guid>(type: "uuid", nullable: true),
                ListId = table.Column<Guid>(type: "uuid", nullable: true),
                ListUnionId = table.Column<Guid>(type: "uuid", nullable: true),
                ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                Type = table.Column<int>(type: "integer", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
                InitialNumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                AllListNumbers = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HagenbachBischoffGroup", x => x.Id);
                table.ForeignKey(
                    name: "FK_HagenbachBischoffGroup_ProportionalElectionEndResult_EndRes~",
                    column: x => x.EndResultId,
                    principalTable: "ProportionalElectionEndResult",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_HagenbachBischoffGroup_ProportionalElectionLists_ListId",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_HagenbachBischoffGroup_ProportionalElectionListUnions_ListU~",
                    column: x => x.ListUnionId,
                    principalTable: "ProportionalElectionListUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PropElectionHBGroup_Parent",
                    column: x => x.ParentId,
                    principalTable: "HagenbachBischoffGroup",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionListUnionEntries",
            columns: table => new
            {
                ProportionalElectionListId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionListUnionId = table.Column<Guid>(type: "uuid", nullable: false),
                Id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionListUnionEntries", x => new { x.ProportionalElectionListId, x.ProportionalElectionListUnionId });
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnionEntries_ProportionalElectionL~1",
                    column: x => x.ProportionalElectionListUnionId,
                    principalTable: "ProportionalElectionListUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnionEntries_ProportionalElectionLi~",
                    column: x => x.ProportionalElectionListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionListUnionTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionListUnionId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionListUnionTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionListUnionTranslations_ProportionalElect~",
                    column: x => x.ProportionalElectionListUnionId,
                    principalTable: "ProportionalElectionListUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionResultBallots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                MarkedForReview = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionResultBallots", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionResultBallots_ProportionalElectionBundl~",
                    column: x => x.BundleId,
                    principalTable: "ProportionalElectionBundles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionCandidateResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ListResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                EVotingSubTotal_UnmodifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_ModifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_CountOfVotesOnOtherLists = table.Column<int>(type: "integer", nullable: false),
                EVotingSubTotal_CountOfVotesFromAccumulations = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_UnmodifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_ModifiedListVotesCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_CountOfVotesOnOtherLists = table.Column<int>(type: "integer", nullable: false),
                ConventionalSubTotal_CountOfVotesFromAccumulations = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionCandidateResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateResults_CandidateId",
                    column: x => x.CandidateId,
                    principalTable: "ProportionalElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateResults_ProportionalElectionLi~",
                    column: x => x.ListResultId,
                    principalTable: "ProportionalElectionListResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionBallotGroupEntryCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PrimaryElectionCandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                SecondaryElectionCandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                BallotGroupEntryId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionBallotGroupEntryCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntryCandidates_MajorityElectio~1",
                    column: x => x.PrimaryElectionCandidateId,
                    principalTable: "MajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntryCandidates_MajorityElection~",
                    column: x => x.BallotGroupEntryId,
                    principalTable: "MajorityElectionBallotGroupEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionBallotGroupEntryCandidates_SecondaryMajorit~",
                    column: x => x.SecondaryElectionCandidateId,
                    principalTable: "SecondaryMajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionCandidateTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecondaryMajorityElectionCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                Occupation = table.Column<string>(type: "text", nullable: false),
                OccupationTitle = table.Column<string>(type: "text", nullable: false),
                Party = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionCandidateTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidateTranslations_SecondaryMaj~",
                    column: x => x.SecondaryMajorityElectionCandidateId,
                    principalTable: "SecondaryMajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionCandidateEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecondaryMajorityElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                Rank = table.Column<int>(type: "integer", nullable: false),
                LotDecision = table.Column<bool>(type: "boolean", nullable: false),
                LotDecisionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                LotDecisionRequired = table.Column<bool>(type: "boolean", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                ConventionalVoteCount = table.Column<int>(type: "integer", nullable: false),
                EVotingVoteCount = table.Column<int>(type: "integer", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionCandidateEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidateEndResults_SecondaryMajo~1",
                    column: x => x.SecondaryMajorityElectionEndResultId,
                    principalTable: "SecondaryMajorityElectionEndResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidateEndResults_SecondaryMajor~",
                    column: x => x.CandidateId,
                    principalTable: "SecondaryMajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionCandidateResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ElectionResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                ConventionalVoteCount = table.Column<int>(type: "integer", nullable: true),
                EVotingVoteCount = table.Column<int>(type: "integer", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionCandidateResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidateResults_SecondaryMajorit~1",
                    column: x => x.ElectionResultId,
                    principalTable: "SecondaryMajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionCandidateResults_SecondaryMajority~",
                    column: x => x.CandidateId,
                    principalTable: "SecondaryMajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionResultBallotCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                Selected = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionResultBallotCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionResultBallotCandidates_MajorityElectionCand~",
                    column: x => x.CandidateId,
                    principalTable: "MajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionResultBallotCandidates_MajorityElectionResu~",
                    column: x => x.BallotId,
                    principalTable: "MajorityElectionResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionResultBallots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PrimaryBallotId = table.Column<Guid>(type: "uuid", nullable: false),
                SecondaryMajorityElectionResultId = table.Column<Guid>(type: "uuid", nullable: false),
                EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                IndividualVoteCount = table.Column<int>(type: "integer", nullable: false),
                InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
                CandidateVoteCountExclIndividual = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionResultBallots", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionResultBallots_MajorityElectionResu~",
                    column: x => x.PrimaryBallotId,
                    principalTable: "MajorityElectionResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionResultBallots_SecondaryMajorityEle~",
                    column: x => x.SecondaryMajorityElectionResultId,
                    principalTable: "SecondaryMajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionCandidateVoteSourceEndResult",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateResultId = table.Column<Guid>(type: "uuid", nullable: false),
                ListId = table.Column<Guid>(type: "uuid", nullable: true),
                EVotingVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalVoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionCandidateVoteSourceEndResult", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateVoteSourceEndResult_Proportio~1",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateVoteSourceEndResult_Proportion~",
                    column: x => x.CandidateResultId,
                    principalTable: "ProportionalElectionCandidateEndResult",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "HagenbachBischoffCalculationRound",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                Index = table.Column<int>(type: "integer", nullable: false),
                WinnerId = table.Column<Guid>(type: "uuid", nullable: false),
                WinnerReason = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HagenbachBischoffCalculationRound", x => x.Id);
                table.ForeignKey(
                    name: "FK_HagenbachBischoffCalculationRound_HagenbachBischoffGroup_Gr~",
                    column: x => x.GroupId,
                    principalTable: "HagenbachBischoffGroup",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_HagenbachBischoffCalculationRound_HagenbachBischoffGroup_Wi~",
                    column: x => x.WinnerId,
                    principalTable: "HagenbachBischoffGroup",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionResultBallotCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                OnList = table.Column<bool>(type: "boolean", nullable: false),
                RemovedFromList = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionResultBallotCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionResultBallotCandidates_ProportionalEle~1",
                    column: x => x.CandidateId,
                    principalTable: "ProportionalElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionResultBallotCandidates_ProportionalElec~",
                    column: x => x.BallotId,
                    principalTable: "ProportionalElectionResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionCandidateVoteSourceResult",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateResultId = table.Column<Guid>(type: "uuid", nullable: false),
                ListId = table.Column<Guid>(type: "uuid", nullable: true),
                EVotingVoteCount = table.Column<int>(type: "integer", nullable: false),
                ConventionalVoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionCandidateVoteSourceResult", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateVoteSourceResult_Proportional~1",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ProportionalElectionCandidateVoteSourceResult_ProportionalE~",
                    column: x => x.CandidateResultId,
                    principalTable: "ProportionalElectionCandidateResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionWriteInMappings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateResultId = table.Column<Guid>(type: "uuid", nullable: true),
                WriteInCandidateName = table.Column<string>(type: "text", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
                Target = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionWriteInMappings", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionWriteInMappings_SecondaryMajority~1",
                    column: x => x.ResultId,
                    principalTable: "SecondaryMajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionWriteInMappings_SecondaryMajorityE~",
                    column: x => x.CandidateResultId,
                    principalTable: "SecondaryMajorityElectionCandidateResults",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionResultBallotCandidates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                Selected = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionResultBallotCandidates", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionResultBallotCandidates_SecondaryM~1",
                    column: x => x.CandidateId,
                    principalTable: "SecondaryMajorityElectionCandidates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionResultBallotCandidates_SecondaryMa~",
                    column: x => x.BallotId,
                    principalTable: "SecondaryMajorityElectionResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "HagenbachBischoffCalculationRoundGroupValues",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                CalculationRoundId = table.Column<Guid>(type: "uuid", nullable: false),
                NextQuotient = table.Column<decimal>(type: "numeric", nullable: false),
                PreviousQuotient = table.Column<decimal>(type: "numeric", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                PreviousNumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                IsWinner = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HagenbachBischoffCalculationRoundGroupValues", x => x.Id);
                table.ForeignKey(
                    name: "FK_HagenbachBischoffCalculationRoundGroupValues_HagenbachBisc~1",
                    column: x => x.GroupId,
                    principalTable: "HagenbachBischoffGroup",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_HagenbachBischoffCalculationRoundGroupValues_HagenbachBisch~",
                    column: x => x.CalculationRoundId,
                    principalTable: "HagenbachBischoffCalculationRound",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Authorities_CountingCircleId",
            table: "Authorities",
            column: "CountingCircleId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotEndResults_BallotId",
            table: "BallotEndResults",
            column: "BallotId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotEndResults_BallotId_VoteEndResultId",
            table: "BallotEndResults",
            columns: new[] { "BallotId", "VoteEndResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotEndResults_VoteEndResultId",
            table: "BallotEndResults",
            column: "VoteEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestionEndResults_BallotEndResultId_QuestionId",
            table: "BallotQuestionEndResults",
            columns: new[] { "BallotEndResultId", "QuestionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestionEndResults_QuestionId",
            table: "BallotQuestionEndResults",
            column: "QuestionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestionResults_BallotResultId",
            table: "BallotQuestionResults",
            column: "BallotResultId");

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestionResults_QuestionId",
            table: "BallotQuestionResults",
            column: "QuestionId");

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestions_BallotId",
            table: "BallotQuestions",
            column: "BallotId");

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestions_Number_BallotId",
            table: "BallotQuestions",
            columns: new[] { "Number", "BallotId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotQuestionTranslations_BallotQuestionId_Language",
            table: "BallotQuestionTranslations",
            columns: new[] { "BallotQuestionId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotResults_BallotId_VoteResultId",
            table: "BallotResults",
            columns: new[] { "BallotId", "VoteResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotResults_VoteResultId",
            table: "BallotResults",
            column: "VoteResultId");

        migrationBuilder.CreateIndex(
            name: "IX_Ballots_VoteId_Position",
            table: "Ballots",
            columns: new[] { "VoteId", "Position" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BallotTranslations_BallotId_Language",
            table: "BallotTranslations",
            columns: new[] { "BallotId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CantonSettings_Canton",
            table: "CantonSettings",
            column: "Canton",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CantonSettingsVotingCardChannels_CantonSettingsId_Valid_Vot~",
            table: "CantonSettingsVotingCardChannels",
            columns: new[] { "CantonSettingsId", "Valid", "VotingChannel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComparisonCountOfVotersConfigurations_PlausibilisationConfi~",
            table: "ComparisonCountOfVotersConfigurations",
            columns: new[] { "PlausibilisationConfigurationId", "Category" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComparisonVoterParticipationConfigurations_Plausibilisation~",
            table: "ComparisonVoterParticipationConfigurations",
            columns: new[] { "PlausibilisationConfigurationId", "MainLevel", "ComparisonLevel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComparisonVotingChannelConfigurations_PlausibilisationConfi~",
            table: "ComparisonVotingChannelConfigurations",
            columns: new[] { "PlausibilisationConfigurationId", "VotingChannel" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountingCircleDetails_ContestId_CountingCircleId",
            table: "ContestCountingCircleDetails",
            columns: new[] { "ContestId", "CountingCircleId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountingCircleDetails_CountingCircleId",
            table: "ContestCountingCircleDetails",
            column: "CountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountOfVotersInformationSubTotals_ContestDetailsId_S~",
            table: "ContestCountOfVotersInformationSubTotals",
            columns: new[] { "ContestDetailsId", "Sex", "VoterType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContestDetails_ContestId",
            table: "ContestDetails",
            column: "ContestId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Contests_DomainOfInfluenceId",
            table: "Contests",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_Contests_PreviousContestId",
            table: "Contests",
            column: "PreviousContestId");

        migrationBuilder.CreateIndex(
            name: "IX_Contests_State",
            table: "Contests",
            column: "State");

        migrationBuilder.CreateIndex(
            name: "IX_ContestTranslations_ContestId_Language",
            table: "ContestTranslations",
            columns: new[] { "ContestId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContestVotingCardResultDetails_ContestDetailsId_Channel_Val~",
            table: "ContestVotingCardResultDetails",
            columns: new[] { "ContestDetailsId", "Channel", "Valid", "DomainOfInfluenceType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleContactPersons_CountingCircleAfterEventId",
            table: "CountingCircleContactPersons",
            column: "CountingCircleAfterEventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleContactPersons_CountingCircleDuringEventId",
            table: "CountingCircleContactPersons",
            column: "CountingCircleDuringEventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleResultComments_ResultId",
            table: "CountingCircleResultComments",
            column: "ResultId");

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircles_SnapshotContestId",
            table: "CountingCircles",
            column: "SnapshotContestId");

        migrationBuilder.CreateIndex(
            name: "IX_CountOfVotersInformationSubTotals_ContestCountingCircleDeta~",
            table: "CountOfVotersInformationSubTotals",
            columns: new[] { "ContestCountingCircleDetailsId", "Sex", "VoterType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCantonDefaultsVotingCardChannels_DomainOfI~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels",
            column: "DomainOfInfluenceCantonDefaultsDomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircles_CountingCircleId_DomainOfI~",
            table: "DomainOfInfluenceCountingCircles",
            columns: new[] { "CountingCircleId", "DomainOfInfluenceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircles_DomainOfInfluenceId",
            table: "DomainOfInfluenceCountingCircles",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceParties_BaseDomainOfInfluencePartyId_Snaps~",
            table: "DomainOfInfluenceParties",
            columns: new[] { "BaseDomainOfInfluencePartyId", "SnapshotContestId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceParties_DomainOfInfluenceId",
            table: "DomainOfInfluenceParties",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceParties_SnapshotContestId",
            table: "DomainOfInfluenceParties",
            column: "SnapshotContestId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluencePartyTranslations_DomainOfInfluencePartyId~",
            table: "DomainOfInfluencePartyTranslations",
            columns: new[] { "DomainOfInfluencePartyId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluencePermissions_ContestId",
            table: "DomainOfInfluencePermissions",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluencePermissions_TenantId_BasisDomainOfInfluenc~",
            table: "DomainOfInfluencePermissions",
            columns: new[] { "TenantId", "BasisDomainOfInfluenceId", "ContestId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluences_ParentId",
            table: "DomainOfInfluences",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluences_SnapshotContestId",
            table: "DomainOfInfluences",
            column: "SnapshotContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ElectionGroups_PrimaryMajorityElectionId",
            table: "ElectionGroups",
            column: "PrimaryMajorityElectionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExportConfigurations_DomainOfInfluenceId",
            table: "ExportConfigurations",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffCalculationRound_GroupId",
            table: "HagenbachBischoffCalculationRound",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffCalculationRound_Index_GroupId",
            table: "HagenbachBischoffCalculationRound",
            columns: new[] { "Index", "GroupId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffCalculationRound_WinnerId",
            table: "HagenbachBischoffCalculationRound",
            column: "WinnerId");

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffCalculationRoundGroupValues_CalculationRou~",
            table: "HagenbachBischoffCalculationRoundGroupValues",
            column: "CalculationRoundId");

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffCalculationRoundGroupValues_GroupId_Calcul~",
            table: "HagenbachBischoffCalculationRoundGroupValues",
            columns: new[] { "GroupId", "CalculationRoundId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffGroup_EndResultId",
            table: "HagenbachBischoffGroup",
            column: "EndResultId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffGroup_ListId",
            table: "HagenbachBischoffGroup",
            column: "ListId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffGroup_ListUnionId",
            table: "HagenbachBischoffGroup",
            column: "ListUnionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_HagenbachBischoffGroup_ParentId",
            table: "HagenbachBischoffGroup",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntries_BallotGroupId",
            table: "MajorityElectionBallotGroupEntries",
            column: "BallotGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntries_PrimaryMajorityElectionId",
            table: "MajorityElectionBallotGroupEntries",
            column: "PrimaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntries_SecondaryMajorityElectio~",
            table: "MajorityElectionBallotGroupEntries",
            column: "SecondaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntryCandidates_BallotGroupEntry~",
            table: "MajorityElectionBallotGroupEntryCandidates",
            column: "BallotGroupEntryId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntryCandidates_PrimaryElectionC~",
            table: "MajorityElectionBallotGroupEntryCandidates",
            column: "PrimaryElectionCandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupEntryCandidates_SecondaryElectio~",
            table: "MajorityElectionBallotGroupEntryCandidates",
            column: "SecondaryElectionCandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupResults_BallotGroupId_ElectionRe~",
            table: "MajorityElectionBallotGroupResults",
            columns: new[] { "BallotGroupId", "ElectionResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroupResults_ElectionResultId",
            table: "MajorityElectionBallotGroupResults",
            column: "ElectionResultId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionBallotGroups_MajorityElectionId",
            table: "MajorityElectionBallotGroups",
            column: "MajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidateEndResults_CandidateId",
            table: "MajorityElectionCandidateEndResults",
            column: "CandidateId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidateEndResults_CandidateId_MajorityEle~",
            table: "MajorityElectionCandidateEndResults",
            columns: new[] { "CandidateId", "MajorityElectionEndResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidateEndResults_MajorityElectionEndResu~",
            table: "MajorityElectionCandidateEndResults",
            column: "MajorityElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidateResults_CandidateId",
            table: "MajorityElectionCandidateResults",
            column: "CandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidateResults_ElectionResultId_Candidate~",
            table: "MajorityElectionCandidateResults",
            columns: new[] { "ElectionResultId", "CandidateId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidates_MajorityElectionId",
            table: "MajorityElectionCandidates",
            column: "MajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidateTranslations_MajorityElectionCandi~",
            table: "MajorityElectionCandidateTranslations",
            columns: new[] { "MajorityElectionCandidateId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionEndResults_MajorityElectionId",
            table: "MajorityElectionEndResults",
            column: "MajorityElectionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBallotCandidates_BallotId_CandidateId",
            table: "MajorityElectionResultBallotCandidates",
            columns: new[] { "BallotId", "CandidateId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBallotCandidates_CandidateId",
            table: "MajorityElectionResultBallotCandidates",
            column: "CandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBallots_BundleId",
            table: "MajorityElectionResultBallots",
            column: "BundleId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBundles_ElectionResultId",
            table: "MajorityElectionResultBundles",
            column: "ElectionResultId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResults_CountingCircleId",
            table: "MajorityElectionResults",
            column: "CountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResults_MajorityElectionId_CountingCircleId",
            table: "MajorityElectionResults",
            columns: new[] { "MajorityElectionId", "CountingCircleId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElections_ContestId",
            table: "MajorityElections",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElections_DomainOfInfluenceId",
            table: "MajorityElections",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionTranslations_MajorityElectionId_Language",
            table: "MajorityElectionTranslations",
            columns: new[] { "MajorityElectionId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionUnionEntries_MajorityElectionId_MajorityEle~",
            table: "MajorityElectionUnionEntries",
            columns: new[] { "MajorityElectionId", "MajorityElectionUnionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionUnionEntries_MajorityElectionUnionId",
            table: "MajorityElectionUnionEntries",
            column: "MajorityElectionUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionUnions_ContestId",
            table: "MajorityElectionUnions",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionWriteInMappings_CandidateResultId",
            table: "MajorityElectionWriteInMappings",
            column: "CandidateResultId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionWriteInMappings_ResultId",
            table: "MajorityElectionWriteInMappings",
            column: "ResultId");

        migrationBuilder.CreateIndex(
            name: "IX_PlausibilisationConfigurations_DomainOfInfluenceId",
            table: "PlausibilisationConfigurations",
            column: "DomainOfInfluenceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionBundles_ElectionResultId",
            table: "ProportionalElectionBundles",
            column: "ElectionResultId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionBundles_ListId",
            table: "ProportionalElectionBundles",
            column: "ListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateEndResult_CandidateId",
            table: "ProportionalElectionCandidateEndResult",
            column: "CandidateId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateEndResult_CandidateId_ListEndR~",
            table: "ProportionalElectionCandidateEndResult",
            columns: new[] { "CandidateId", "ListEndResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateEndResult_ListEndResultId",
            table: "ProportionalElectionCandidateEndResult",
            column: "ListEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateResults_CandidateId_ListResult~",
            table: "ProportionalElectionCandidateResults",
            columns: new[] { "CandidateId", "ListResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateResults_ListResultId",
            table: "ProportionalElectionCandidateResults",
            column: "ListResultId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidates_PartyId",
            table: "ProportionalElectionCandidates",
            column: "PartyId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidates_ProportionalElectionListId",
            table: "ProportionalElectionCandidates",
            column: "ProportionalElectionListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateTranslations_ProportionalElect~",
            table: "ProportionalElectionCandidateTranslations",
            columns: new[] { "ProportionalElectionCandidateId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateVoteSourceEndResult_CandidateR~",
            table: "ProportionalElectionCandidateVoteSourceEndResult",
            columns: new[] { "CandidateResultId", "ListId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateVoteSourceEndResult_ListId",
            table: "ProportionalElectionCandidateVoteSourceEndResult",
            column: "ListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateVoteSourceResult_CandidateResu~",
            table: "ProportionalElectionCandidateVoteSourceResult",
            columns: new[] { "CandidateResultId", "ListId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionCandidateVoteSourceResult_ListId",
            table: "ProportionalElectionCandidateVoteSourceResult",
            column: "ListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionEndResult_ProportionalElectionId",
            table: "ProportionalElectionEndResult",
            column: "ProportionalElectionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListEndResult_ElectionEndResultId",
            table: "ProportionalElectionListEndResult",
            column: "ElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListEndResult_ListId",
            table: "ProportionalElectionListEndResult",
            column: "ListId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListEndResult_ListId_ElectionEndResultId",
            table: "ProportionalElectionListEndResult",
            columns: new[] { "ListId", "ElectionEndResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListResults_ListId",
            table: "ProportionalElectionListResults",
            column: "ListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListResults_ResultId_ListId",
            table: "ProportionalElectionListResults",
            columns: new[] { "ResultId", "ListId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionLists_ProportionalElectionId",
            table: "ProportionalElectionLists",
            column: "ProportionalElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListTranslations_ProportionalElectionLi~",
            table: "ProportionalElectionListTranslations",
            columns: new[] { "ProportionalElectionListId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnionEntries_ProportionalElectionLi~",
            table: "ProportionalElectionListUnionEntries",
            column: "ProportionalElectionListUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnions_ProportionalElectionId",
            table: "ProportionalElectionListUnions",
            column: "ProportionalElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnions_ProportionalElectionMainList~",
            table: "ProportionalElectionListUnions",
            column: "ProportionalElectionMainListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnions_ProportionalElectionRootList~",
            table: "ProportionalElectionListUnions",
            column: "ProportionalElectionRootListUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionListUnionTranslations_ProportionalElect~",
            table: "ProportionalElectionListUnionTranslations",
            columns: new[] { "ProportionalElectionListUnionId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionResultBallotCandidates_BallotId",
            table: "ProportionalElectionResultBallotCandidates",
            column: "BallotId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionResultBallotCandidates_CandidateId",
            table: "ProportionalElectionResultBallotCandidates",
            column: "CandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionResultBallots_BundleId",
            table: "ProportionalElectionResultBallots",
            column: "BundleId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionResults_CountingCircleId",
            table: "ProportionalElectionResults",
            column: "CountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionResults_ProportionalElectionId_Counting~",
            table: "ProportionalElectionResults",
            columns: new[] { "ProportionalElectionId", "CountingCircleId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElections_ContestId",
            table: "ProportionalElections",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElections_DomainOfInfluenceId",
            table: "ProportionalElections",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionTranslations_ProportionalElectionId_Lan~",
            table: "ProportionalElectionTranslations",
            columns: new[] { "ProportionalElectionId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionEntries_ProportionalElectionId_Pro~",
            table: "ProportionalElectionUnionEntries",
            columns: new[] { "ProportionalElectionId", "ProportionalElectionUnionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionEntries_ProportionalElectionUnionId",
            table: "ProportionalElectionUnionEntries",
            column: "ProportionalElectionUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionListEntries_ProportionalElectionLi~",
            table: "ProportionalElectionUnionListEntries",
            columns: new[] { "ProportionalElectionListId", "ProportionalElectionUnionListId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionListEntries_ProportionalElectionUn~",
            table: "ProportionalElectionUnionListEntries",
            column: "ProportionalElectionUnionListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionLists_ProportionalElectionUnionId",
            table: "ProportionalElectionUnionLists",
            column: "ProportionalElectionUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionListTranslations_ProportionalElect~",
            table: "ProportionalElectionUnionListTranslations",
            columns: new[] { "ProportionalElectionUnionListId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnions_ContestId",
            table: "ProportionalElectionUnions",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnmodifiedListResults_ListId",
            table: "ProportionalElectionUnmodifiedListResults",
            column: "ListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnmodifiedListResults_ResultId_ListId",
            table: "ProportionalElectionUnmodifiedListResults",
            columns: new[] { "ResultId", "ListId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ResultExportConfigurationPoliticalBusinesses_PoliticalBusin~",
            table: "ResultExportConfigurationPoliticalBusinesses",
            columns: new[] { "PoliticalBusinessId", "ResultExportConfigurationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ResultExportConfigurationPoliticalBusinesses_ResultExportCo~",
            table: "ResultExportConfigurationPoliticalBusinesses",
            column: "ResultExportConfigurationId");

        migrationBuilder.CreateIndex(
            name: "IX_ResultExportConfigurations_ContestId",
            table: "ResultExportConfigurations",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ResultExportConfigurations_DomainOfInfluenceId",
            table: "ResultExportConfigurations",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_ResultImports_ContestId",
            table: "ResultImports",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidateEndResults_CandidateId",
            table: "SecondaryMajorityElectionCandidateEndResults",
            column: "CandidateId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidateEndResults_CandidateId_Se~",
            table: "SecondaryMajorityElectionCandidateEndResults",
            columns: new[] { "CandidateId", "SecondaryMajorityElectionEndResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidateEndResults_SecondaryMajor~",
            table: "SecondaryMajorityElectionCandidateEndResults",
            column: "SecondaryMajorityElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidateResults_CandidateId",
            table: "SecondaryMajorityElectionCandidateResults",
            column: "CandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidateResults_ElectionResultId_~",
            table: "SecondaryMajorityElectionCandidateResults",
            columns: new[] { "ElectionResultId", "CandidateId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidates_CandidateReferenceId",
            table: "SecondaryMajorityElectionCandidates",
            column: "CandidateReferenceId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidates_SecondaryMajorityElecti~",
            table: "SecondaryMajorityElectionCandidates",
            column: "SecondaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionCandidateTranslations_SecondaryMaj~",
            table: "SecondaryMajorityElectionCandidateTranslations",
            columns: new[] { "SecondaryMajorityElectionCandidateId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionEndResults_PrimaryMajorityElection~",
            table: "SecondaryMajorityElectionEndResults",
            column: "PrimaryMajorityElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionEndResults_SecondaryMajorityElect~1",
            table: "SecondaryMajorityElectionEndResults",
            columns: new[] { "SecondaryMajorityElectionId", "PrimaryMajorityElectionEndResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionEndResults_SecondaryMajorityElecti~",
            table: "SecondaryMajorityElectionEndResults",
            column: "SecondaryMajorityElectionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionResultBallotCandidates_BallotId_Ca~",
            table: "SecondaryMajorityElectionResultBallotCandidates",
            columns: new[] { "BallotId", "CandidateId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionResultBallotCandidates_CandidateId",
            table: "SecondaryMajorityElectionResultBallotCandidates",
            column: "CandidateId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionResultBallots_PrimaryBallotId_Seco~",
            table: "SecondaryMajorityElectionResultBallots",
            columns: new[] { "PrimaryBallotId", "SecondaryMajorityElectionResultId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionResultBallots_SecondaryMajorityEle~",
            table: "SecondaryMajorityElectionResultBallots",
            column: "SecondaryMajorityElectionResultId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionResults_PrimaryResultId",
            table: "SecondaryMajorityElectionResults",
            column: "PrimaryResultId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionResults_SecondaryMajorityElectionId",
            table: "SecondaryMajorityElectionResults",
            column: "SecondaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElections_ElectionGroupId",
            table: "SecondaryMajorityElections",
            column: "ElectionGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElections_PrimaryMajorityElectionId",
            table: "SecondaryMajorityElections",
            column: "PrimaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionTranslations_SecondaryMajorityElec~",
            table: "SecondaryMajorityElectionTranslations",
            columns: new[] { "SecondaryMajorityElectionId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionWriteInMappings_CandidateResultId",
            table: "SecondaryMajorityElectionWriteInMappings",
            column: "CandidateResultId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionWriteInMappings_ResultId",
            table: "SecondaryMajorityElectionWriteInMappings",
            column: "ResultId");

        migrationBuilder.CreateIndex(
            name: "IX_SimpleCountingCircleResults_CountingCircleId",
            table: "SimpleCountingCircleResults",
            column: "CountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_SimpleCountingCircleResults_PoliticalBusinessId",
            table: "SimpleCountingCircleResults",
            column: "PoliticalBusinessId");

        migrationBuilder.CreateIndex(
            name: "IX_SimplePoliticalBusinesses_ContestId",
            table: "SimplePoliticalBusinesses",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_SimplePoliticalBusinesses_DomainOfInfluenceId",
            table: "SimplePoliticalBusinesses",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_SimplePoliticalBusinessTranslations_SimplePoliticalBusiness~",
            table: "SimplePoliticalBusinessTranslations",
            columns: new[] { "SimplePoliticalBusinessId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TieBreakQuestionEndResults_BallotEndResultId",
            table: "TieBreakQuestionEndResults",
            column: "BallotEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_TieBreakQuestionEndResults_QuestionId",
            table: "TieBreakQuestionEndResults",
            column: "QuestionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TieBreakQuestionResults_BallotResultId",
            table: "TieBreakQuestionResults",
            column: "BallotResultId");

        migrationBuilder.CreateIndex(
            name: "IX_TieBreakQuestionResults_QuestionId",
            table: "TieBreakQuestionResults",
            column: "QuestionId");

        migrationBuilder.CreateIndex(
            name: "IX_TieBreakQuestions_BallotId_Question1Number_Question2Number",
            table: "TieBreakQuestions",
            columns: new[] { "BallotId", "Question1Number", "Question2Number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TieBreakQuestionTranslations_TieBreakQuestionId_Language",
            table: "TieBreakQuestionTranslations",
            columns: new[] { "TieBreakQuestionId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VoteEndResults_VoteId",
            table: "VoteEndResults",
            column: "VoteId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VoteResultBallotQuestionAnswers_BallotId_QuestionId",
            table: "VoteResultBallotQuestionAnswers",
            columns: new[] { "BallotId", "QuestionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VoteResultBallotQuestionAnswers_QuestionId",
            table: "VoteResultBallotQuestionAnswers",
            column: "QuestionId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteResultBallots_BundleId",
            table: "VoteResultBallots",
            column: "BundleId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteResultBallotTieBreakQuestionAnswers_BallotId_QuestionId",
            table: "VoteResultBallotTieBreakQuestionAnswers",
            columns: new[] { "BallotId", "QuestionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VoteResultBallotTieBreakQuestionAnswers_QuestionId",
            table: "VoteResultBallotTieBreakQuestionAnswers",
            column: "QuestionId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteResultBundles_BallotResultId",
            table: "VoteResultBundles",
            column: "BallotResultId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteResults_CountingCircleId",
            table: "VoteResults",
            column: "CountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteResults_VoteId_CountingCircleId",
            table: "VoteResults",
            columns: new[] { "VoteId", "CountingCircleId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Votes_ContestId",
            table: "Votes",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_Votes_DomainOfInfluenceId",
            table: "Votes",
            column: "DomainOfInfluenceId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteTranslations_VoteId_Language",
            table: "VoteTranslations",
            columns: new[] { "VoteId", "Language" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VotingCardResultDetails_ContestCountingCircleDetailsId_Chan~",
            table: "VotingCardResultDetails",
            columns: new[] { "ContestCountingCircleDetailsId", "Channel", "Valid", "DomainOfInfluenceType" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Authorities_CountingCircles_CountingCircleId",
            table: "Authorities",
            column: "CountingCircleId",
            principalTable: "CountingCircles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_BallotQuestions_Ballots_BallotId",
            table: "BallotQuestions",
            column: "BallotId",
            principalTable: "Ballots",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_BallotQuestionEndResults_BallotEndResults_BallotEndResultId",
            table: "BallotQuestionEndResults",
            column: "BallotEndResultId",
            principalTable: "BallotEndResults",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_BallotQuestionResults_BallotResults_BallotResultId",
            table: "BallotQuestionResults",
            column: "BallotResultId",
            principalTable: "BallotResults",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Ballots_Votes_VoteId",
            table: "Ballots",
            column: "VoteId",
            principalTable: "Votes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_BallotEndResults_VoteEndResults_VoteEndResultId",
            table: "BallotEndResults",
            column: "VoteEndResultId",
            principalTable: "VoteEndResults",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_BallotResults_VoteResults_VoteResultId",
            table: "BallotResults",
            column: "VoteResultId",
            principalTable: "VoteResults",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ComparisonCountOfVotersConfigurations_PlausibilisationConfi~",
            table: "ComparisonCountOfVotersConfigurations",
            column: "PlausibilisationConfigurationId",
            principalTable: "PlausibilisationConfigurations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ComparisonVoterParticipationConfigurations_Plausibilisation~",
            table: "ComparisonVoterParticipationConfigurations",
            column: "PlausibilisationConfigurationId",
            principalTable: "PlausibilisationConfigurations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ComparisonVotingChannelConfigurations_PlausibilisationConfi~",
            table: "ComparisonVotingChannelConfigurations",
            column: "PlausibilisationConfigurationId",
            principalTable: "PlausibilisationConfigurations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ContestCountOfVotersInformationSubTotals_ContestDetails_Con~",
            table: "ContestCountOfVotersInformationSubTotals",
            column: "ContestDetailsId",
            principalTable: "ContestDetails",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ContestDetails_Contests_ContestId",
            table: "ContestDetails",
            column: "ContestId",
            principalTable: "Contests",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Contests_DomainOfInfluences_DomainOfInfluenceId",
            table: "Contests",
            column: "DomainOfInfluenceId",
            principalTable: "DomainOfInfluences",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DomainOfInfluences_Contests_SnapshotContestId",
            table: "DomainOfInfluences");

        migrationBuilder.DropTable(
            name: "Authorities");

        migrationBuilder.DropTable(
            name: "BallotQuestionEndResults");

        migrationBuilder.DropTable(
            name: "BallotQuestionResults");

        migrationBuilder.DropTable(
            name: "BallotQuestionTranslations");

        migrationBuilder.DropTable(
            name: "BallotTranslations");

        migrationBuilder.DropTable(
            name: "CantonSettingsVotingCardChannels");

        migrationBuilder.DropTable(
            name: "ComparisonCountOfVotersConfigurations");

        migrationBuilder.DropTable(
            name: "ComparisonVoterParticipationConfigurations");

        migrationBuilder.DropTable(
            name: "ComparisonVotingChannelConfigurations");

        migrationBuilder.DropTable(
            name: "ContestCountOfVotersInformationSubTotals");

        migrationBuilder.DropTable(
            name: "ContestTranslations");

        migrationBuilder.DropTable(
            name: "ContestVotingCardResultDetails");

        migrationBuilder.DropTable(
            name: "CountingCircleContactPersons");

        migrationBuilder.DropTable(
            name: "CountingCircleResultComments");

        migrationBuilder.DropTable(
            name: "CountOfVotersInformationSubTotals");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceCantonDefaultsVotingCardChannels");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceCountingCircles");

        migrationBuilder.DropTable(
            name: "DomainOfInfluencePartyTranslations");

        migrationBuilder.DropTable(
            name: "DomainOfInfluencePermissions");

        migrationBuilder.DropTable(
            name: "EventProcessingStates");

        migrationBuilder.DropTable(
            name: "ExportConfigurations");

        migrationBuilder.DropTable(
            name: "HagenbachBischoffCalculationRoundGroupValues");

        migrationBuilder.DropTable(
            name: "MajorityElectionBallotGroupEntryCandidates");

        migrationBuilder.DropTable(
            name: "MajorityElectionBallotGroupResults");

        migrationBuilder.DropTable(
            name: "MajorityElectionCandidateEndResults");

        migrationBuilder.DropTable(
            name: "MajorityElectionCandidateTranslations");

        migrationBuilder.DropTable(
            name: "MajorityElectionResultBallotCandidates");

        migrationBuilder.DropTable(
            name: "MajorityElectionTranslations");

        migrationBuilder.DropTable(
            name: "MajorityElectionUnionEntries");

        migrationBuilder.DropTable(
            name: "MajorityElectionWriteInMappings");

        migrationBuilder.DropTable(
            name: "ProportionalElectionCandidateTranslations");

        migrationBuilder.DropTable(
            name: "ProportionalElectionCandidateVoteSourceEndResult");

        migrationBuilder.DropTable(
            name: "ProportionalElectionCandidateVoteSourceResult");

        migrationBuilder.DropTable(
            name: "ProportionalElectionListTranslations");

        migrationBuilder.DropTable(
            name: "ProportionalElectionListUnionEntries");

        migrationBuilder.DropTable(
            name: "ProportionalElectionListUnionTranslations");

        migrationBuilder.DropTable(
            name: "ProportionalElectionResultBallotCandidates");

        migrationBuilder.DropTable(
            name: "ProportionalElectionTranslations");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnionEntries");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnionListEntries");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnionListTranslations");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnmodifiedListResults");

        migrationBuilder.DropTable(
            name: "ResultExportConfigurationPoliticalBusinesses");

        migrationBuilder.DropTable(
            name: "ResultImports");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionCandidateEndResults");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionCandidateTranslations");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionResultBallotCandidates");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionTranslations");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionWriteInMappings");

        migrationBuilder.DropTable(
            name: "SimplePoliticalBusinessTranslations");

        migrationBuilder.DropTable(
            name: "TieBreakQuestionEndResults");

        migrationBuilder.DropTable(
            name: "TieBreakQuestionResults");

        migrationBuilder.DropTable(
            name: "TieBreakQuestionTranslations");

        migrationBuilder.DropTable(
            name: "VoteResultBallotQuestionAnswers");

        migrationBuilder.DropTable(
            name: "VoteResultBallotTieBreakQuestionAnswers");

        migrationBuilder.DropTable(
            name: "VoteTranslations");

        migrationBuilder.DropTable(
            name: "VotingCardResultDetails");

        migrationBuilder.DropTable(
            name: "CantonSettings");

        migrationBuilder.DropTable(
            name: "PlausibilisationConfigurations");

        migrationBuilder.DropTable(
            name: "ContestDetails");

        migrationBuilder.DropTable(
            name: "SimpleCountingCircleResults");

        migrationBuilder.DropTable(
            name: "HagenbachBischoffCalculationRound");

        migrationBuilder.DropTable(
            name: "MajorityElectionBallotGroupEntries");

        migrationBuilder.DropTable(
            name: "MajorityElectionUnions");

        migrationBuilder.DropTable(
            name: "MajorityElectionCandidateResults");

        migrationBuilder.DropTable(
            name: "ProportionalElectionCandidateEndResult");

        migrationBuilder.DropTable(
            name: "ProportionalElectionCandidateResults");

        migrationBuilder.DropTable(
            name: "ProportionalElectionResultBallots");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnionLists");

        migrationBuilder.DropTable(
            name: "ResultExportConfigurations");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionEndResults");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionResultBallots");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionCandidateResults");

        migrationBuilder.DropTable(
            name: "BallotEndResults");

        migrationBuilder.DropTable(
            name: "BallotQuestions");

        migrationBuilder.DropTable(
            name: "TieBreakQuestions");

        migrationBuilder.DropTable(
            name: "VoteResultBallots");

        migrationBuilder.DropTable(
            name: "ContestCountingCircleDetails");

        migrationBuilder.DropTable(
            name: "SimplePoliticalBusinesses");

        migrationBuilder.DropTable(
            name: "HagenbachBischoffGroup");

        migrationBuilder.DropTable(
            name: "MajorityElectionBallotGroups");

        migrationBuilder.DropTable(
            name: "ProportionalElectionListEndResult");

        migrationBuilder.DropTable(
            name: "ProportionalElectionCandidates");

        migrationBuilder.DropTable(
            name: "ProportionalElectionListResults");

        migrationBuilder.DropTable(
            name: "ProportionalElectionBundles");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnions");

        migrationBuilder.DropTable(
            name: "MajorityElectionEndResults");

        migrationBuilder.DropTable(
            name: "MajorityElectionResultBallots");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionResults");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropTable(
            name: "VoteEndResults");

        migrationBuilder.DropTable(
            name: "VoteResultBundles");

        migrationBuilder.DropTable(
            name: "ProportionalElectionListUnions");

        migrationBuilder.DropTable(
            name: "ProportionalElectionEndResult");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceParties");

        migrationBuilder.DropTable(
            name: "ProportionalElectionResults");

        migrationBuilder.DropTable(
            name: "MajorityElectionResultBundles");

        migrationBuilder.DropTable(
            name: "MajorityElectionCandidates");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElections");

        migrationBuilder.DropTable(
            name: "BallotResults");

        migrationBuilder.DropTable(
            name: "ProportionalElectionLists");

        migrationBuilder.DropTable(
            name: "MajorityElectionResults");

        migrationBuilder.DropTable(
            name: "ElectionGroups");

        migrationBuilder.DropTable(
            name: "Ballots");

        migrationBuilder.DropTable(
            name: "VoteResults");

        migrationBuilder.DropTable(
            name: "ProportionalElections");

        migrationBuilder.DropTable(
            name: "MajorityElections");

        migrationBuilder.DropTable(
            name: "CountingCircles");

        migrationBuilder.DropTable(
            name: "Votes");

        migrationBuilder.DropTable(
            name: "Contests");

        migrationBuilder.DropTable(
            name: "DomainOfInfluences");
    }
}
