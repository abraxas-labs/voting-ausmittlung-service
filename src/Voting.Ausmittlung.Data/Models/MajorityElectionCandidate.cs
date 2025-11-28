// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Extensions;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionCandidate : MajorityElectionCandidateBase
{
    public Guid MajorityElectionId { get; set; }

    public override Guid PoliticalBusinessId => MajorityElectionId;

    public MajorityElection MajorityElection { get; set; } = null!;

    public ICollection<MajorityElectionCandidateTranslation> Translations { get; set; } = new HashSet<MajorityElectionCandidateTranslation>();

    public ICollection<SecondaryMajorityElectionCandidate> CandidateReferences { get; set; } = new HashSet<SecondaryMajorityElectionCandidate>();

    public ICollection<MajorityElectionCandidate> CandidateReferencesOfSecondaryElectionsOnSeparateBallot { get; set; } = new HashSet<MajorityElectionCandidate>();

    public ICollection<MajorityElectionBallotGroupEntryCandidate> BallotGroupEntries { get; set; } = new HashSet<MajorityElectionBallotGroupEntryCandidate>();

    public ICollection<MajorityElectionCandidateResult> CandidateResults { get; set; } =
        new HashSet<MajorityElectionCandidateResult>();

    public ICollection<MajorityElectionResultBallotCandidate> BallotCandidatures { get; set; }
        = new HashSet<MajorityElectionResultBallotCandidate>();

    public MajorityElectionCandidateEndResult? EndResult { get; set; }

    /// <summary>
    /// Gets or sets the id of the referenced candidate.
    /// This can only be set for secondary elections on separate ballots.
    /// </summary>
    public Guid? CandidateReferenceId { get; set; }

    public MajorityElectionCandidate? CandidateReference { get; set; }

    public override string PartyShortDescription => Translations.GetTranslated(x => x.PartyShortDescription, true);

    public override string PartyLongDescription => Translations.GetTranslated(x => x.PartyLongDescription, true);

    public override string Occupation => Translations.GetTranslated(x => x.Occupation, true);

    public override string OccupationTitle => Translations.GetTranslated(x => x.OccupationTitle, true);
}
