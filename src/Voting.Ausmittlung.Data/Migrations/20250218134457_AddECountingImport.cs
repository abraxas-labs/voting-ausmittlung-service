// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddECountingImport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "SimpleCountingCircleResults",
            newName: "CountOfVoters_EVotingSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "SimpleCountingCircleResults",
            newName: "CountOfVoters_EVotingSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "SimpleCountingCircleResults",
            newName: "CountOfVoters_EVotingSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "SimpleCountingCircleResults",
            newName: "CountOfVoters_EVotingSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "SimpleCountingCircleResults",
            newName: "CountOfVoters_ConventionalSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "SimpleCountingCircleResults",
            newName: "CountOfVoters_ConventionalSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "SimpleCountingCircleResults",
            newName: "CountOfVoters_ConventionalSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "SimpleCountingCircleResults",
            newName: "CountOfVoters_ConventionalSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "ProportionalElectionResults",
            newName: "CountOfVoters_EVotingSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "ProportionalElectionResults",
            newName: "CountOfVoters_EVotingSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "ProportionalElectionResults",
            newName: "CountOfVoters_EVotingSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "ProportionalElectionResults",
            newName: "CountOfVoters_EVotingSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "ProportionalElectionResults",
            newName: "CountOfVoters_ConventionalSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "ProportionalElectionResults",
            newName: "CountOfVoters_ConventionalSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "ProportionalElectionResults",
            newName: "CountOfVoters_ConventionalSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "ProportionalElectionResults",
            newName: "CountOfVoters_ConventionalSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "ProportionalElectionEndResult",
            newName: "CountOfVoters_EVotingSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "ProportionalElectionEndResult",
            newName: "CountOfVoters_EVotingSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "ProportionalElectionEndResult",
            newName: "CountOfVoters_EVotingSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "ProportionalElectionEndResult",
            newName: "CountOfVoters_EVotingSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "ProportionalElectionEndResult",
            newName: "CountOfVoters_ConventionalSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "ProportionalElectionEndResult",
            newName: "CountOfVoters_ConventionalSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "ProportionalElectionEndResult",
            newName: "CountOfVoters_ConventionalSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "ProportionalElectionEndResult",
            newName: "CountOfVoters_ConventionalSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "MajorityElectionResults",
            newName: "CountOfVoters_EVotingSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "MajorityElectionResults",
            newName: "CountOfVoters_EVotingSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "MajorityElectionResults",
            newName: "CountOfVoters_EVotingSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "MajorityElectionResults",
            newName: "CountOfVoters_EVotingSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "MajorityElectionResults",
            newName: "CountOfVoters_ConventionalSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "MajorityElectionResults",
            newName: "CountOfVoters_ConventionalSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "MajorityElectionResults",
            newName: "CountOfVoters_ConventionalSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "MajorityElectionResults",
            newName: "CountOfVoters_ConventionalSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "MajorityElectionEndResults",
            newName: "CountOfVoters_EVotingSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "MajorityElectionEndResults",
            newName: "CountOfVoters_EVotingSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "MajorityElectionEndResults",
            newName: "CountOfVoters_EVotingSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "MajorityElectionEndResults",
            newName: "CountOfVoters_EVotingSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "MajorityElectionEndResults",
            newName: "CountOfVoters_ConventionalSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "MajorityElectionEndResults",
            newName: "CountOfVoters_ConventionalSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "MajorityElectionEndResults",
            newName: "CountOfVoters_ConventionalSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "MajorityElectionEndResults",
            newName: "CountOfVoters_ConventionalSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "BallotResults",
            newName: "CountOfVoters_EVotingSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "BallotResults",
            newName: "CountOfVoters_EVotingSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "BallotResults",
            newName: "CountOfVoters_EVotingSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "BallotResults",
            newName: "CountOfVoters_EVotingSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "BallotResults",
            newName: "CountOfVoters_ConventionalSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "BallotResults",
            newName: "CountOfVoters_ConventionalSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "BallotResults",
            newName: "CountOfVoters_ConventionalSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "BallotResults",
            newName: "CountOfVoters_ConventionalSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "BallotEndResults",
            newName: "CountOfVoters_EVotingSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "BallotEndResults",
            newName: "CountOfVoters_EVotingSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "BallotEndResults",
            newName: "CountOfVoters_EVotingSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "BallotEndResults",
            newName: "CountOfVoters_EVotingSubTotal_AccountedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "BallotEndResults",
            newName: "CountOfVoters_ConventionalSubTotal_ReceivedBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "BallotEndResults",
            newName: "CountOfVoters_ConventionalSubTotal_InvalidBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "BallotEndResults",
            newName: "CountOfVoters_ConventionalSubTotal_BlankBallots");

        migrationBuilder.RenameColumn(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "BallotEndResults",
            newName: "CountOfVoters_ConventionalSubTotal_AccountedBallots");

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerQ1",
            table: "TieBreakQuestionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerQ2",
            table: "TieBreakQuestionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerUnspecified",
            table: "TieBreakQuestionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerQ1",
            table: "TieBreakQuestionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerQ2",
            table: "TieBreakQuestionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerUnspecified",
            table: "TieBreakQuestionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_AccountedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_BlankBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_InvalidBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_ReceivedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_EmptyVoteCountExclWriteIns",
            table: "SecondaryMajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_IndividualVoteCount",
            table: "SecondaryMajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_InvalidVoteCount",
            table: "SecondaryMajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCandidateVoteCountExclIndividual",
            table: "SecondaryMajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_EmptyVoteCountExclWriteIns",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_IndividualVoteCount",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_InvalidVoteCount",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCandidateVoteCountExclIndividual",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingExclWriteInsVoteCount",
            table: "SecondaryMajorityElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingWriteInsVoteCount",
            table: "SecondaryMajorityElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingVoteCount",
            table: "SecondaryMajorityElectionCandidateEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "CountingCircleId",
            table: "ResultImports",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ImportType",
            table: "ResultImports",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingVoteCount",
            table: "ProportionalElectionUnmodifiedListResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfUnmodifiedLists",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfModifiedLists",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfListsWithoutParty",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfBlankRowsOnListsWithoutParty",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_BlankBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_AccountedBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_InvalidBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_ReceivedBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ListVotesCountOnOtherLists",
            table: "ProportionalElectionListResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ModifiedListBlankRowsCount",
            table: "ProportionalElectionListResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ModifiedListVotesCount",
            table: "ProportionalElectionListResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ModifiedListsCount",
            table: "ProportionalElectionListResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_UnmodifiedListBlankRowsCount",
            table: "ProportionalElectionListResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_UnmodifiedListVotesCount",
            table: "ProportionalElectionListResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_UnmodifiedListsCount",
            table: "ProportionalElectionListResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ListVotesCountOnOtherLists",
            table: "ProportionalElectionListEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ModifiedListBlankRowsCount",
            table: "ProportionalElectionListEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ModifiedListVotesCount",
            table: "ProportionalElectionListEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ModifiedListsCount",
            table: "ProportionalElectionListEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_UnmodifiedListBlankRowsCount",
            table: "ProportionalElectionListEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_UnmodifiedListVotesCount",
            table: "ProportionalElectionListEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_UnmodifiedListsCount",
            table: "ProportionalElectionListEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfUnmodifiedLists",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfModifiedLists",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfListsWithoutParty",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfBlankRowsOnListsWithoutParty",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_BlankBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_AccountedBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_InvalidBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_ReceivedBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingVoteCount",
            table: "ProportionalElectionCandidateVoteSourceResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingVoteCount",
            table: "ProportionalElectionCandidateVoteSourceEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_CountOfVotesFromAccumulations",
            table: "ProportionalElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_CountOfVotesOnOtherLists",
            table: "ProportionalElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ModifiedListVotesCount",
            table: "ProportionalElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_UnmodifiedListVotesCount",
            table: "ProportionalElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_CountOfVotesFromAccumulations",
            table: "ProportionalElectionCandidateEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_CountOfVotesOnOtherLists",
            table: "ProportionalElectionCandidateEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_ModifiedListVotesCount",
            table: "ProportionalElectionCandidateEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_UnmodifiedListVotesCount",
            table: "ProportionalElectionCandidateEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_IndividualVoteCount",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_InvalidVoteCount",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCandidateVoteCountExclIndividual",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_EmptyVoteCountExclWriteIns",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_AccountedBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_BlankBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_InvalidBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_ReceivedBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_AccountedBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_BlankBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_InvalidBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_ReceivedBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_IndividualVoteCount",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_InvalidVoteCount",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCandidateVoteCountExclIndividual",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_EmptyVoteCountExclWriteIns",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingExclWriteInsVoteCount",
            table: "MajorityElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingWriteInsVoteCount",
            table: "MajorityElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingVoteCount",
            table: "MajorityElectionCandidateEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "ECounting",
            table: "CountingCircles",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "NumberOfCountingCirclesWithECountingImported",
            table: "Contests",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "ECounting",
            table: "ContestCountingCircleDetails",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ECountingResultsImported",
            table: "ContestCountingCircleDetails",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_AccountedBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_BlankBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_InvalidBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_ReceivedBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerNo",
            table: "BallotQuestionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerUnspecified",
            table: "BallotQuestionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerYes",
            table: "BallotQuestionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerNo",
            table: "BallotQuestionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerUnspecified",
            table: "BallotQuestionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ECountingSubTotal_TotalCountOfAnswerYes",
            table: "BallotQuestionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_AccountedBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_BlankBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_InvalidBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ECountingSubTotal_ReceivedBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_ResultImports_CountingCircleId",
            table: "ResultImports",
            column: "CountingCircleId");

        migrationBuilder.AddForeignKey(
            name: "FK_ResultImports_CountingCircles_CountingCircleId",
            table: "ResultImports",
            column: "CountingCircleId",
            principalTable: "CountingCircles",
            principalColumn: "Id");

        // set import type = EVoting for all existing imports
        migrationBuilder.Sql("""
                             UPDATE "ResultImports"
                             SET "ImportType" = 1
                             """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new InvalidOperationException("Down is not supported by this migration!");
    }
}
