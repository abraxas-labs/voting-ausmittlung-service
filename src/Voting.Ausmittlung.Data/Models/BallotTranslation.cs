// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class BallotTranslation : TranslationEntity
{
    public string ShortDescription { get; set; } = string.Empty;

    public string OfficialDescription { get; set; } = string.Empty;

    public Guid BallotId { get; set; }

    public Ballot? Ballot { get; set; }
}
