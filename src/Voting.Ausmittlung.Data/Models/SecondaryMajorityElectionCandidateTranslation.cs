// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionCandidateTranslation : TranslationEntity
{
    public Guid SecondaryMajorityElectionCandidateId { get; set; }

    public SecondaryMajorityElectionCandidate? SecondaryMajorityElectionCandidate { get; set; }

    public string Occupation { get; set; } = string.Empty;

    public string OccupationTitle { get; set; } = string.Empty;

    public string Party { get; set; } = string.Empty;
}
