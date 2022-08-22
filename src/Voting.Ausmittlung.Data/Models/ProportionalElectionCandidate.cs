// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Extensions;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionCandidate : ElectionCandidate
{
    public bool Accumulated { get; set; }

    public int AccumulatedPosition { get; set; }

    public Guid ProportionalElectionListId { get; set; }

    public ProportionalElectionList ProportionalElectionList { get; set; } = null!;

    public ICollection<ProportionalElectionCandidateTranslation> Translations { get; set; } = new HashSet<ProportionalElectionCandidateTranslation>();

    public ICollection<ProportionalElectionResultBallotCandidate> BallotCandidatures { get; set; }
        = new HashSet<ProportionalElectionResultBallotCandidate>();

    public ICollection<ProportionalElectionCandidateResult> Results { get; set; } =
        new HashSet<ProportionalElectionCandidateResult>();

    public ProportionalElectionCandidateEndResult? EndResult { get; set; }

    public Guid? PartyId { get; set; }

    /// <summary>
    /// Gets or sets the contest snapshot domain of influence party.
    /// </summary>
    public DomainOfInfluenceParty? Party { get; set; }

    public string NumberIncludingList => $"{ProportionalElectionList.OrderNumber}.{Number}";

    public string Description => $"{NumberIncludingList} {PoliticalLastName} {PoliticalFirstName}";

    public override string Occupation => Translations.GetTranslated(x => x.Occupation, true);

    public override string OccupationTitle => Translations.GetTranslated(x => x.OccupationTitle, true);
}
