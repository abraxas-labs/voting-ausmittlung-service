// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionUnionListTranslation : TranslationEntity
{
    public string ShortDescription { get; set; } = string.Empty;

    public Guid ProportionalElectionUnionListId { get; set; }

    public ProportionalElectionUnionList? ProportionalElectionUnionList { get; set; }
}
