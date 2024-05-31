// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionList : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public int BlankRowCount { get; set; }

    public int Position { get; set; }

    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!;

    public ICollection<ProportionalElectionListTranslation> Translations { get; set; }
        = new HashSet<ProportionalElectionListTranslation>();

    public ICollection<ProportionalElectionCandidate> ProportionalElectionCandidates { get; set; }
        = new HashSet<ProportionalElectionCandidate>();

    public ICollection<ProportionalElectionListUnionEntry> ProportionalElectionListUnionEntries { get; set; }
        = new HashSet<ProportionalElectionListUnionEntry>();

    public ICollection<ProportionalElectionListUnion> ProportionalElectionMainListUnions { get; set; }
        = new HashSet<ProportionalElectionListUnion>();

    public ICollection<ProportionalElectionUnionListEntry> ProportionalElectionUnionListEntries { get; set; }
        = new HashSet<ProportionalElectionUnionListEntry>();

    public ICollection<ProportionalElectionUnmodifiedListResult> UnmodifiedListResults { get; set; }
        = new HashSet<ProportionalElectionUnmodifiedListResult>();

    public ICollection<ProportionalElectionListResult> Results { get; set; }
        = new HashSet<ProportionalElectionListResult>();

    public ICollection<ProportionalElectionResultBundle> Bundles { get; set; } =
        new HashSet<ProportionalElectionResultBundle>();

    public ICollection<ProportionalElectionCandidateVoteSourceEndResult> CandidateEndResultVoteSources { get; set; }
        = new HashSet<ProportionalElectionCandidateVoteSourceEndResult>();

    public ICollection<ProportionalElectionCandidateVoteSourceResult> CandidateResultVoteSources { get; set; }
        = new HashSet<ProportionalElectionCandidateVoteSourceResult>();

    public HagenbachBischoffGroup? HagenbachBischoffGroup { get; set; }

    public ProportionalElectionListEndResult? EndResult { get; set; }

    public ICollection<DoubleProportionalResultCell> DoubleProportionalResultCells { get; set; } = new HashSet<DoubleProportionalResultCell>();

    public DoubleProportionalResultColumn? DoubleProportionalResultColumn { get; set; }

    // TODO: Simplify with jira ticket 398
    [NotMapped]
    public ProportionalElectionListUnion? ProportionalElectionListUnion =>
        ProportionalElectionListUnionEntries
            .FirstOrDefault(x => !x.ProportionalElectionListUnion.IsSubListUnion)?
            .ProportionalElectionListUnion;

    [NotMapped]
    public ProportionalElectionListUnion? ProportionalElectionSubListUnion =>
        ProportionalElectionListUnionEntries
            .FirstOrDefault(x => x.ProportionalElectionListUnion.IsSubListUnion)?
            .ProportionalElectionListUnion;

    public string Description => Translations.GetTranslated(x => x.Description);

    public string ShortDescription => Translations.GetTranslated(x => x.ShortDescription);
}
