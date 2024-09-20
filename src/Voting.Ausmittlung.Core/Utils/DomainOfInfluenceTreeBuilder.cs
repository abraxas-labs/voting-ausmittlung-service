// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils;

public static class DomainOfInfluenceTreeBuilder
{
    internal static List<DomainOfInfluence> BuildTree(List<DomainOfInfluence> flatDomains)
    {
        if (flatDomains.Count == 0)
        {
            return flatDomains;
        }

        var byParentId = flatDomains
            .GroupBy(x => x.ParentId ?? Guid.Empty)
            .ToDictionary(x => x.Key, x => x.ToList()); // empty guid for roots
        var byId = flatDomains
            .ToDictionary(x => x.Id);

        foreach (var (parentId, dois) in byParentId)
        {
            if (parentId == Guid.Empty)
            {
                continue;
            }

            var parent = byId[parentId];
            foreach (var doi in dois.OrderBy(doi => doi.Name))
            {
                doi.Parent = parent;
                parent.Children.Add(doi);
            }
        }

        return byParentId[Guid.Empty].OrderBy(d => d.Name).ToList();
    }
}
