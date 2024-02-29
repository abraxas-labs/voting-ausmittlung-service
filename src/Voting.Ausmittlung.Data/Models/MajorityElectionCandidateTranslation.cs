// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionCandidateTranslation : TranslationEntity
{
    public Guid MajorityElectionCandidateId { get; set; }

    public MajorityElectionCandidate? MajorityElectionCandidate { get; set; }

    public string Occupation { get; set; } = string.Empty;

    public string OccupationTitle { get; set; } = string.Empty;

    public string Party { get; set; } = string.Empty;
}
