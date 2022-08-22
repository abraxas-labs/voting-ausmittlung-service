// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluenceParty : BaseEntity, IHasSnapshotContest
{
    // The id of the VOTING Basis DomainOfInfluenceParty
    public Guid BaseDomainOfInfluencePartyId { get; set; }

    public Guid DomainOfInfluenceId { get; set; }

    public DomainOfInfluence DomainOfInfluence { get; set; } = null!;

    public ICollection<ProportionalElectionCandidate> ProportionalElectionCandidates { get; set; } = new HashSet<ProportionalElectionCandidate>();

    public bool Deleted { get; set; }

    public ICollection<DomainOfInfluencePartyTranslation> Translations { get; set; } = new HashSet<DomainOfInfluencePartyTranslation>();

    public string Name => Translations.GetTranslated(t => t.Name);

    public string ShortDescription => Translations.GetTranslated(t => t.ShortDescription);

    public Contest? SnapshotContest { get; set; }

    // The contest id of the contest, for which this DomainOfInfluenceParty was "snapshotted"
    public Guid? SnapshotContestId { get; set; }
}
