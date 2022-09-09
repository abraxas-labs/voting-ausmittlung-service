// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// this wraps a list, a listUnion or a listSubUnion as a hirarchial tree.
/// ex:
/// - List A
/// - ListUnion 1
///   - List B
///   - ListSubUnion 2
///     - List C
///     - List D.
/// </summary>
public class HagenbachBischoffGroup : BaseEntity
{
    private string? _allListNumbers;

    public ProportionalElectionEndResult? EndResult { get; set; }

    public Guid? EndResultId { get; set; }

    public ProportionalElectionList? List { get; set; }

    public Guid? ListId { get; set; }

    public ProportionalElectionListUnion? ListUnion { get; set; }

    public Guid? ListUnionId { get; set; }

    public Guid? ParentId { get; set; }

    public HagenbachBischoffGroup? Parent { get; set; }

    public HagenbachBischoffGroupType Type { get; set; }

    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the number of mandates gained from the initial distribution.
    /// </summary>
    public int InitialNumberOfMandates { get; set; }

    /// <summary>
    /// Gets or sets the number of mandates of the latest calculation round.
    /// </summary>
    public int NumberOfMandates { get; set; }

    /// <summary>
    /// Gets the quotient.
    /// </summary>
    public decimal Quotient => (decimal)VoteCount / (NumberOfMandates + 1);

    /// <summary>
    /// Gets the next higher integer of the quotient.
    /// </summary>
    public int DistributionNumber => Quotient % 1 == 0
        ? (int)Quotient + 1
        : (int)Math.Ceiling(Quotient);

    public ICollection<HagenbachBischoffGroup> Children { get; set; }
        = new HashSet<HagenbachBischoffGroup>();

    public ICollection<HagenbachBischoffCalculationRound> CalculationRounds { get; set; }
        = new HashSet<HagenbachBischoffCalculationRound>();

    public ICollection<HagenbachBischoffCalculationRound> CalculationWinnerRounds { get; set; }
        = new HashSet<HagenbachBischoffCalculationRound>();

    public ICollection<HagenbachBischoffCalculationRoundGroupValues> CalculationRoundValues { get; set; }
        = new HashSet<HagenbachBischoffCalculationRoundGroupValues>();

    [NotMapped]
    public IEnumerable<HagenbachBischoffGroup> AllGroups
    {
        get
        {
            yield return this;
            foreach (var child in ChildrenOrdered.SelectMany(x => x.AllGroups))
            {
                yield return child;
            }
        }
    }

    [NotMapped]
    public IEnumerable<HagenbachBischoffGroup> ChildrenOrdered
        => Children
            .OrderBy(x => x.Type)
            .ThenBy(x => x.ListUnion?.Position ?? int.MinValue)
            .ThenBy(x => x.List?.Position ?? int.MinValue);

    [NotMapped]
    public IEnumerable<ProportionalElectionList> AllLists
        => AllGroups
            .Where(x => x.List != null)
            .Select(x => x.List!);

    public string? Description => List?.Description ?? ListUnion?.Description;

    public string? ShortDescription => List?.ShortDescription ?? ListUnion?.Description;

    /// <summary>
    /// Gets or sets all list numbers separated by comma.
    /// This field is stored in the DB, that it is queryable and available without loading all children.
    /// </summary>
    public string AllListNumbers
    {
        get => _allListNumbers ??= string.Join(
                ",",
                AllLists
                    .OrderBy(x => x!.Position)
                    .Select(x => x!.OrderNumber));
        set => _allListNumbers = value;
    }

    public void SortCalculationRounds()
    {
        CalculationRounds = CalculationRounds.OrderBy(x => x.Index).ToList();

        foreach (var calculationRound in CalculationRounds)
        {
            calculationRound.GroupValues = calculationRound.GroupValues
                .OrderBy(x => x.Group?.Type ?? HagenbachBischoffGroupType.Root)
                .ThenBy(x => x.Group?.ListUnion?.Position ?? int.MinValue)
                .ThenBy(x => x.Group?.List?.Position ?? int.MinValue)
                .ToList();
        }

        foreach (var child in Children)
        {
            child.SortCalculationRounds();
        }
    }
}
