// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionCandidateTranslation : TranslationEntity
{
    public Guid ProportionalElectionCandidateId { get; set; }

    public ProportionalElectionCandidate? ProportionalElectionCandidate { get; set; }

    public string Occupation { get; set; } = string.Empty;

    public string OccupationTitle { get; set; } = string.Empty;
}
