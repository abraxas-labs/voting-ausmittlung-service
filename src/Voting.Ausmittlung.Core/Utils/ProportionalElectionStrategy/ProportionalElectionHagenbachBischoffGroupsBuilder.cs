// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.ProportionalElectionStrategy;

public static class ProportionalElectionHagenbachBischoffGroupsBuilder
{
    public static HagenbachBischoffGroup BuildHagenbachBischoffGroups(ProportionalElectionEndResult endResult)
    {
        var lists = endResult.ListEndResults.Select(x => x.List).ToList();
        var group = new HagenbachBischoffGroup
        {
            Type = HagenbachBischoffGroupType.Root,
            Children = BuildHagenbachBischoffListGroups(lists),
        };
        RecalculateVoteCount(group);
        return group;
    }

    private static List<HagenbachBischoffGroup> BuildHagenbachBischoffListGroups(
        List<ProportionalElectionList> lists)
    {
        var groupsById = new Dictionary<Guid, HagenbachBischoffGroup>();
        foreach (var list in lists)
        {
            var listUnion = list.ProportionalElectionListUnion;
            var subListUnion = list.ProportionalElectionSubListUnion;
            var group = groupsById[list.Id] = BuildGroup(list, subListUnion?.Id ?? listUnion?.Id);
            if (subListUnion != null)
            {
                AddToSubListUnion(groupsById, group, subListUnion);
            }
            else if (listUnion != null)
            {
                AddToListUnion(groupsById, group, listUnion.Id);
            }
        }

        return groupsById.Values
            .Where(x => !x.ParentId.HasValue)
            .ToList();
    }

    private static void AddToSubListUnion(
        Dictionary<Guid, HagenbachBischoffGroup> groupsById,
        HagenbachBischoffGroup list,
        ProportionalElectionListUnion subListUnion)
    {
        if (!groupsById.TryGetValue(subListUnion.Id, out var subListUnionGroup))
        {
            subListUnionGroup
                = groupsById[subListUnion.Id]
                    = new HagenbachBischoffGroup
                    {
                        Type = HagenbachBischoffGroupType.SubListUnion,
                        ListUnionId = subListUnion.Id,
                        ParentId = subListUnion.ProportionalElectionRootListUnionId,
                    };
            AddToListUnion(groupsById, subListUnionGroup, subListUnion.ProportionalElectionRootListUnionId!.Value);
        }

        subListUnionGroup.Children.Add(list);
    }

    private static void AddToListUnion(
        Dictionary<Guid, HagenbachBischoffGroup> groupsById,
        HagenbachBischoffGroup child,
        Guid listUnionId)
    {
        if (!groupsById.TryGetValue(listUnionId, out var listUnionGroup))
        {
            listUnionGroup
                = groupsById[listUnionId]
                    = new HagenbachBischoffGroup
                    {
                        Type = HagenbachBischoffGroupType.ListUnion,
                        ListUnionId = listUnionId,
                    };
        }

        listUnionGroup.Children.Add(child);
    }

    private static HagenbachBischoffGroup BuildGroup(
        ProportionalElectionList list,
        Guid? parentId = null)
    {
        return new HagenbachBischoffGroup
        {
            Type = HagenbachBischoffGroupType.List,
            ListId = list.Id,
            List = list,
            ParentId = parentId,
            VoteCount = list.EndResult?.TotalVoteCount ?? 0,
        };
    }

    private static void RecalculateVoteCount(IEnumerable<HagenbachBischoffGroup> groups)
    {
        foreach (var group in groups)
        {
            RecalculateVoteCount(group);
        }
    }

    private static void RecalculateVoteCount(HagenbachBischoffGroup group)
    {
        RecalculateVoteCount(group.Children);
        group.VoteCount += group.Children.Sum(x => x.VoteCount);
    }
}
