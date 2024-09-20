// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Extensions;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionCandidate : MajorityElectionCandidateBase
{
    public Guid SecondaryMajorityElectionId { get; set; }

    public override Guid PoliticalBusinessId => SecondaryMajorityElectionId;

    public SecondaryMajorityElection SecondaryMajorityElection { get; set; } = null!;

    public ICollection<SecondaryMajorityElectionCandidateTranslation> Translations { get; set; } = new HashSet<SecondaryMajorityElectionCandidateTranslation>();

    public Guid? CandidateReferenceId { get; set; }

    public MajorityElectionCandidate? CandidateReference { get; set; }

    public ICollection<MajorityElectionBallotGroupEntryCandidate> BallotGroupEntries { get; set; }
        = new HashSet<MajorityElectionBallotGroupEntryCandidate>();

    public ICollection<SecondaryMajorityElectionResultBallotCandidate> BallotCandidatures { get; set; }
        = new HashSet<SecondaryMajorityElectionResultBallotCandidate>();

    public ICollection<SecondaryMajorityElectionCandidateResult> CandidateResults { get; set; } =
        new HashSet<SecondaryMajorityElectionCandidateResult>();

    public SecondaryMajorityElectionCandidateEndResult? EndResult { get; set; }

    public override string Party => Translations.GetTranslated(x => x.Party);

    public override string Occupation => Translations.GetTranslated(x => x.Occupation, true);

    public override string OccupationTitle => Translations.GetTranslated(x => x.OccupationTitle, true);
}
