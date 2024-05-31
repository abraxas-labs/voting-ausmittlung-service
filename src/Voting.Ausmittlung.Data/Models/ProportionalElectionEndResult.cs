// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionEndResult : PoliticalBusinessEndResultBase,
    IEndResultDetail<ProportionalElectionEndResultCountOfVotersInformationSubTotal, ProportionalElectionEndResultVotingCardDetail>,
    IHasSubTotals<ProportionalElectionResultSubTotal>,
    IProportionalElectionResultTotal
{
    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!;

    public ICollection<ProportionalElectionEndResultCountOfVotersInformationSubTotal> CountOfVotersInformationSubTotals { get; set; }
        = new HashSet<ProportionalElectionEndResultCountOfVotersInformationSubTotal>();

    public ICollection<ProportionalElectionEndResultVotingCardDetail> VotingCards { get; set; }
        = new HashSet<ProportionalElectionEndResultVotingCardDetail>();

    /// <summary>
    /// Gets or sets the count of voters, meaning the persons who actually voted in this election.
    /// In German: Wahlzettel.
    /// </summary>
    public PoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new();

    public ICollection<ProportionalElectionListEndResult> ListEndResults { get; set; } =
        new HashSet<ProportionalElectionListEndResult>();

    public ProportionalElectionResultSubTotal EVotingSubTotal { get; set; } = new();

    public ProportionalElectionResultSubTotal ConventionalSubTotal { get; set; } = new();

    /// <summary>
    /// Gets the total count of unmodified lists with a party.
    /// </summary>
    public int TotalCountOfUnmodifiedLists => EVotingSubTotal.TotalCountOfUnmodifiedLists + ConventionalSubTotal.TotalCountOfUnmodifiedLists;

    /// <summary>
    /// Gets the total count of modified lists with a party.
    /// </summary>
    public int TotalCountOfModifiedLists => EVotingSubTotal.TotalCountOfModifiedLists + ConventionalSubTotal.TotalCountOfModifiedLists;

    /// <summary>
    /// Gets the count of lists without a source list / party.
    /// </summary>
    public int TotalCountOfListsWithoutParty => EVotingSubTotal.TotalCountOfListsWithoutParty + ConventionalSubTotal.TotalCountOfListsWithoutParty;

    /// <summary>
    /// Gets the count of ballots (= total count of modified lists with and without a party).
    /// </summary>
    public int TotalCountOfBallots => TotalCountOfModifiedLists + TotalCountOfListsWithoutParty;

    /// <summary>
    /// Gets the count of votes gained from blank rows from lists/ballots without a source list / party.
    /// </summary>
    public int TotalCountOfBlankRowsOnListsWithoutParty => EVotingSubTotal.TotalCountOfBlankRowsOnListsWithoutParty + ConventionalSubTotal.TotalCountOfBlankRowsOnListsWithoutParty;

    /// <summary>
    /// Gets the total count of lists with a party (modified + unmodified).
    /// </summary>
    public int TotalCountOfListsWithParty => TotalCountOfUnmodifiedLists + TotalCountOfModifiedLists;

    /// <summary>
    /// Gets the total count of lists (without and with a party).
    /// </summary>
    public int TotalCountOfLists => TotalCountOfListsWithParty + TotalCountOfListsWithoutParty;

    public HagenbachBischoffGroup? HagenbachBischoffRootGroup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the end result is manual. This happens when the mandate algorithm cannot distribute all mandates.
    /// When it is set all <see cref="ProportionalElectionCandidateEndResultState"/> have to be set manually.
    /// </summary>
    public bool ManualEndResultRequired { get; set; }

    public void ResetCalculation()
    {
        foreach (var listEndResult in ListEndResults)
        {
            listEndResult.NumberOfMandates = 0;
        }
    }

    public void ResetAllSubTotals(VotingDataSource dataSource, bool includeCountOfVoters = false)
    {
        this.ResetSubTotal(dataSource);

        foreach (var listEndResult in ListEndResults)
        {
            listEndResult.ResetAllSubTotals(dataSource);
        }

        if (includeCountOfVoters)
        {
            CountOfVoters.ResetSubTotal(dataSource, TotalCountOfVoters);
        }
    }

    public void Reset()
    {
        ManualEndResultRequired = false;
        Finalized = false;

        foreach (var listEndResult in ListEndResults)
        {
            listEndResult.NumberOfMandates = 0;
            listEndResult.HasOpenRequiredLotDecisions = false;

            foreach (var candidateEndResult in listEndResult.CandidateEndResults)
            {
                candidateEndResult.Rank = 0;
                candidateEndResult.LotDecision = false;
                candidateEndResult.LotDecisionRequired = false;
                candidateEndResult.LotDecisionEnabled = false;
                candidateEndResult.State = ProportionalElectionCandidateEndResultState.Pending;
            }
        }
    }
}
