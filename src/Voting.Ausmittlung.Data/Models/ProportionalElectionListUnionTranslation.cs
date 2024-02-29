// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionListUnionTranslation : TranslationEntity
{
    public Guid ProportionalElectionListUnionId { get; set; }

    public ProportionalElectionListUnion? ProportionalElectionListUnion { get; set; }

    public string Description { get; set; } = string.Empty;
}
