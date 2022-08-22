// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.ProportionalElectionStrategy;

public static class ProportionalElectionHagenbachBischoffStrategy
{
    public static void RecalculateNumberOfMandatesForLists(ProportionalElectionEndResult endResult)
    {
        // list votes count cannot be negative, checked by business rules
        if (endResult.ListEndResults.All(x => x.ListVotesCount == 0))
        {
            return;
        }

        endResult.HagenbachBischoffRootGroup
            = ProportionalElectionHagenbachBischoffGroupsBuilder.BuildHagenbachBischoffGroups(endResult);

        endResult.HagenbachBischoffRootGroup.NumberOfMandates = endResult.ProportionalElection.NumberOfMandates;
        CalculateHagenbachBischoff(endResult.HagenbachBischoffRootGroup);
        endResult.HagenbachBischoffRootGroup.InitialNumberOfMandates
            = endResult.HagenbachBischoffRootGroup.Children.Sum(x => x.InitialNumberOfMandates);

        UpdateNumberOfMandatesForListEndResults(endResult);
    }

    /// <summary>
    /// Calculate the mandates for the election lists with the Hagenbach Bischoff Algorithm. For more Information see
    /// Art. 99 - Art. 101 from <a href="https://www.gesetzessammlung.sg.ch/app/de/texts_of_law/125.3/versions/2500">here</a>..
    /// </summary>
    private static void CalculateHagenbachBischoff(HagenbachBischoffGroup group)
    {
        if (group.Children.Count == 0)
        {
            return;
        }

        // Step 1: Basic distribution
        var distributionNumber = Math.Max(1, group.DistributionNumber);
        var leftOverMandates = group.NumberOfMandates;
        foreach (var childGroup in group.Children)
        {
            childGroup.InitialNumberOfMandates = childGroup.VoteCount / distributionNumber;
            childGroup.NumberOfMandates = childGroup.InitialNumberOfMandates;
            leftOverMandates -= childGroup.InitialNumberOfMandates;
        }

        // Step 2: Distribute rest mandates
        DistributeRestMandates(group, leftOverMandates);

        // Step 3: Calculate Hagenbach Bischoff for list unions and sub list unions
        foreach (var childGroup in group.Children)
        {
            CalculateHagenbachBischoff(childGroup);
        }
    }

    private static void UpdateNumberOfMandatesForListEndResults(ProportionalElectionEndResult result)
    {
        foreach (var group in result.HagenbachBischoffRootGroup!.AllGroups)
        {
            if (!group.ListId.HasValue)
            {
                continue;
            }

            group.List!.EndResult!.NumberOfMandates = group.NumberOfMandates;
        }
    }

    /// <summary>
    /// Calculate the rest mandates according
    /// Art. 100 from <a href="https://www.gesetzessammlung.sg.ch/app/de/texts_of_law/125.3/versions/2500">here.</a>
    /// e) and f) are currently not implemented.
    /// </summary>
    /// <param name="group">Hagenbach Bischoff group.</param>
    /// <param name="restNumberOfMandates">Number of Mandates of the parent Context.</param>
    private static void DistributeRestMandates(
        HagenbachBischoffGroup group,
        int restNumberOfMandates)
    {
        for (var i = 0; i < restNumberOfMandates; i++)
        {
            var groupsWithHighestQuotient = group.Children.MaxsBy(x => x.Quotient).ToList();
            if (groupsWithHighestQuotient.Count == 1)
            {
                SetCalculationRoundWinner(
                    group,
                    groupsWithHighestQuotient[0],
                    HagenbachBischoffCalculationRoundWinnerReason.Quotient);
                continue;
            }

            var groupsWithHighestRest = groupsWithHighestQuotient.MaxsBy(x => x.VoteCount % group.DistributionNumber).ToList();
            if (groupsWithHighestRest.Count == 1)
            {
                SetCalculationRoundWinner(
                    group,
                    groupsWithHighestRest[0],
                    HagenbachBischoffCalculationRoundWinnerReason.QuotientRemainder);
                continue;
            }

            var groupsWithHighestVoteCount = groupsWithHighestRest.MaxsBy(x => x.VoteCount).ToList();
            if (groupsWithHighestVoteCount.Count == 1)
            {
                SetCalculationRoundWinner(
                    group,
                    groupsWithHighestVoteCount[0],
                    HagenbachBischoffCalculationRoundWinnerReason.VoteCount);
            }
        }
    }

    private static void SetCalculationRoundWinner(
        HagenbachBischoffGroup group,
        HagenbachBischoffGroup winner,
        HagenbachBischoffCalculationRoundWinnerReason winnerReason)
    {
        var winnerPreviousQuotient = winner.Quotient;
        winner.NumberOfMandates++;
        group.CalculationRounds.Add(new HagenbachBischoffCalculationRound
        {
            Index = group.CalculationRounds.Count,
            Winner = winner,
            WinnerReason = winnerReason,
            Group = group,
            GroupValues = group.Children.Select(x =>
                x == winner
                    ? new HagenbachBischoffCalculationRoundGroupValues
                    {
                        Group = x,
                        PreviousQuotient = winnerPreviousQuotient,
                        NextQuotient = x.Quotient,
                        PreviousNumberOfMandates = x.NumberOfMandates - 1,
                        NumberOfMandates = x.NumberOfMandates,
                        IsWinner = true,
                    }
                    : new HagenbachBischoffCalculationRoundGroupValues
                    {
                        Group = x,
                        PreviousQuotient = x.Quotient,
                        NextQuotient = x.Quotient,
                        PreviousNumberOfMandates = x.NumberOfMandates,
                        NumberOfMandates = x.NumberOfMandates,
                        IsWinner = false,
                    }).ToList(),
        });
    }
}
