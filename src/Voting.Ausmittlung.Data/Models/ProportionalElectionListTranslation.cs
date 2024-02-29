// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionListTranslation : TranslationEntity
{
    public Guid ProportionalElectionListId { get; set; }

    public ProportionalElectionList? ProportionalElectionList { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
