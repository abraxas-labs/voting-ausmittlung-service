// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionUnionList : BaseEntity
{
    // for ef
    public ProportionalElectionUnionList()
    {
    }

    public ProportionalElectionUnionList(
        Guid unionId,
        string orderNumber,
        ICollection<ProportionalElectionListTranslation> translations,
        List<ProportionalElectionList> lists)
    {
        OrderNumber = orderNumber;
        Translations = translations
            .Select(t => new ProportionalElectionUnionListTranslation { Language = t.Language, ShortDescription = t.ShortDescription })
            .ToList();
        ProportionalElectionUnionId = unionId;
        ProportionalElectionUnionListEntries = lists.Select(l => new ProportionalElectionUnionListEntry
        {
            ProportionalElectionListId = l.Id,
        }).ToList();
    }

    // copied from matching lists
    public string OrderNumber { get; set; } = string.Empty;

    // copied from matching lists
    public ICollection<ProportionalElectionUnionListTranslation> Translations { get; set; }
        = new HashSet<ProportionalElectionUnionListTranslation>();

    public Guid ProportionalElectionUnionId { get; set; }

    public ProportionalElectionUnion ProportionalElectionUnion { get; set; } = null!; // set by ef

    public ICollection<ProportionalElectionUnionListEntry> ProportionalElectionUnionListEntries { get; set; }
        = new HashSet<ProportionalElectionUnionListEntry>();

    [NotMapped]
    public int ListCount => ProportionalElectionUnionListEntries.Count;

    [NotMapped]
    public string PoliticalBusinessNumbers => string.Join(' ', ProportionalElectionUnionListEntries
        .Select(e => e.ProportionalElectionList?.ProportionalElection?.PoliticalBusinessNumber ?? string.Empty)
        .OrderBy(n => n));

    public string ShortDescription => Translations.GetTranslated(x => x.ShortDescription);
}
