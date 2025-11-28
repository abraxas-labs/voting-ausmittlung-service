// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionCandidateBase : ElectionCandidate
{
    public string Description => $"{Number} {PoliticalLastName} {PoliticalFirstName}";

    public abstract string PartyShortDescription { get; }

    public abstract string PartyLongDescription { get; }

    public bool CountToIndividual => ReportingType is MajorityElectionCandidateReportingType.CountToIndividual;

    public abstract Guid PoliticalBusinessId { get; }

    public bool CreatedDuringActiveContest { get; set; }

    public MajorityElectionCandidateReportingType ReportingType { get; set; }
}
