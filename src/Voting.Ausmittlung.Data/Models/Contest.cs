// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class Contest : BaseEntity
{
    public DateTime Date { get; set; }

    public DateTime EndOfTestingPhase { get; set; }

    public ContestState State { get; set; } = ContestState.TestingPhase;

    public Guid DomainOfInfluenceId { get; set; }

    public DomainOfInfluence DomainOfInfluence { get; set; } = null!; // set by ef

    public bool EVotingResultsImported { get; set; }

    public bool EVoting { get; set; }

    public DateTime? EVotingFrom { get; set; }

    public DateTime? EVotingTo { get; set; }

    public ICollection<ContestTranslation> Translations { get; set; } = new HashSet<ContestTranslation>();

    public ContestDetails? Details { get; set; }

    public ICollection<Vote> Votes { get; set; } = new HashSet<Vote>();

    public ICollection<ProportionalElection> ProportionalElections { get; set; } = new HashSet<ProportionalElection>();

    public ICollection<MajorityElection> MajorityElections { get; set; } = new HashSet<MajorityElection>();

    public ICollection<ProportionalElectionUnion> ProportionalElectionUnions { get; set; } = new HashSet<ProportionalElectionUnion>();

    public ICollection<MajorityElectionUnion> MajorityElectionUnions { get; set; } = new HashSet<MajorityElectionUnion>();

    public ICollection<ContestCountingCircleDetails> CountingCircleDetails { get; set; } = new HashSet<ContestCountingCircleDetails>();

    public ICollection<ContestDomainOfInfluenceDetails> DomainOfInfluenceDetails { get; set; } = new HashSet<ContestDomainOfInfluenceDetails>();

    public ICollection<SimplePoliticalBusiness> SimplePoliticalBusinesses { get; set; } = new HashSet<SimplePoliticalBusiness>();

    public ICollection<ResultImport> ResultImports { get; set; } = new HashSet<ResultImport>();

    public ICollection<ResultExportConfiguration> ResultExportConfigurations { get; set; } = new HashSet<ResultExportConfiguration>();

    public Guid? PreviousContestId { get; set; }

    public Contest? PreviousContest { get; set; }

    public ICollection<Contest> PreviousContestOwners { get; set; } = new HashSet<Contest>();

    // snapshotted DomainOfInfluenceParties.
    public ICollection<DomainOfInfluenceParty> DomainOfInfluenceParties { get; set; } = new HashSet<DomainOfInfluenceParty>();

    [NotMapped]
    public IEnumerable<PoliticalBusiness> PoliticalBusinesses
    {
        get
        {
            return Votes.Cast<PoliticalBusiness>()
                .Concat(ProportionalElections)
                .Concat(MajorityElections)
                .OrderBy(pb => pb.DomainOfInfluence.Type)
                .ThenBy(pb => pb.PoliticalBusinessNumber);
        }

        set
        {
            Votes.Clear();
            ProportionalElections.Clear();
            MajorityElections.Clear();

            foreach (var item in value)
            {
                switch (item)
                {
                    case Vote v:
                        Votes.Add(v);
                        break;
                    case ProportionalElection e:
                        ProportionalElections.Add(e);
                        break;
                    case MajorityElection e:
                        MajorityElections.Add(e);
                        break;
                }
            }
        }
    }

    [NotMapped]
    public IEnumerable<PoliticalBusinessUnion> PoliticalBusinessUnions
    {
        get
        {
            return ProportionalElectionUnions.Cast<PoliticalBusinessUnion>()
                .Concat(MajorityElectionUnions)
                .OrderBy(u => u.Description);
        }

        set
        {
            ProportionalElectionUnions.Clear();
            MajorityElectionUnions.Clear();

            foreach (var item in value)
            {
                switch (item)
                {
                    case ProportionalElectionUnion p:
                        ProportionalElectionUnions.Add(p);
                        break;
                    case MajorityElectionUnion m:
                        MajorityElectionUnions.Add(m);
                        break;
                }
            }
        }
    }

    public bool TestingPhaseEnded => State.TestingPhaseEnded();

    public string Description => Translations.GetTranslated(t => t.Description);
}
