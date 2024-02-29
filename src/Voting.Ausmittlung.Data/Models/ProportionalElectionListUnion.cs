// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionListUnion : BaseEntity
{
    public int Position { get; set; }

    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!;

    public ICollection<ProportionalElectionListUnionTranslation> Translations { get; set; } = new HashSet<ProportionalElectionListUnionTranslation>();

    public Guid? ProportionalElectionRootListUnionId { get; set; }

    public ProportionalElectionListUnion? ProportionalElectionRootListUnion { get; set; }

    public Guid? ProportionalElectionMainListId { get; set; }

    public ProportionalElectionList? ProportionalElectionMainList { get; set; }

    public ICollection<ProportionalElectionListUnion> ProportionalElectionSubListUnions { get; set; } = new HashSet<ProportionalElectionListUnion>();

    public ICollection<ProportionalElectionListUnionEntry> ProportionalElectionListUnionEntries { get; set; } = new HashSet<ProportionalElectionListUnionEntry>();

    public bool IsSubListUnion => ProportionalElectionRootListUnionId.HasValue;

    public string AllListNumbers => string.Join(
        ',',
        ProportionalElectionListUnionEntries.Select(x => x.ProportionalElectionList.OrderNumber));

    public HagenbachBischoffGroup? HagenbachBischoffGroup { get; set; }

    public string Description => Translations.GetTranslated(x => x.Description);
}
