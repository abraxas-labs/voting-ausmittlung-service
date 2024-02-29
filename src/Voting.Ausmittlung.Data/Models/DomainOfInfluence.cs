// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluence : BaseEntity, IHasSnapshotContest
{
    public string Name { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public string Bfs { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int SortNumber { get; set; }

    public string NameForProtocol { get; set; } = string.Empty;

    public string SecureConnectId { get; set; } = string.Empty;

    public string AuthorityName { get; set; } = string.Empty;

    public DomainOfInfluenceType Type { get; set; }

    public DomainOfInfluenceCanton Canton { get; set; }

    public Guid? ParentId { get; set; }

    public DomainOfInfluence? Parent { get; set; }

    public ICollection<DomainOfInfluence> Children { get; set; } = new HashSet<DomainOfInfluence>();

    // The id of the VOTING Basis DomainOfInfluence
    public Guid BasisDomainOfInfluenceId { get; set; }

    // The contest id of the contest, for which this DomainOfInfluence was "snapshotted"
    public Guid? SnapshotContestId { get; set; }

    public Contest? SnapshotContest { get; set; }

    public bool IsSnapshot => SnapshotContestId.HasValue;

    public ICollection<DomainOfInfluenceCountingCircle> CountingCircles { get; set; } = new HashSet<DomainOfInfluenceCountingCircle>();

    public ICollection<Contest> Contests { get; set; } = new HashSet<Contest>();

    public ICollection<Vote> Votes { get; set; } = new HashSet<Vote>();

    public ICollection<ProportionalElection> ProportionalElections { get; set; } = new HashSet<ProportionalElection>();

    public ICollection<MajorityElection> MajorityElections { get; set; } = new HashSet<MajorityElection>();

    public ICollection<SimplePoliticalBusiness> SimplePoliticalBusinesses { get; set; } = new HashSet<SimplePoliticalBusiness>();

    public ICollection<ExportConfiguration>? ExportConfigurations { get; set; }

    public ICollection<ResultExportConfiguration>? ResultExportConfigurations { get; set; }

    public ContactPerson ContactPerson { get; set; } = new();

    public DomainOfInfluenceCantonDefaults CantonDefaults { get; set; } = new();

    public PlausibilisationConfiguration? PlausibilisationConfiguration { get; set; }

    public ICollection<DomainOfInfluenceParty> Parties { get; set; } = new HashSet<DomainOfInfluenceParty>();

    public ContestDomainOfInfluenceDetails? Details { get; set; }
}
