// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class BallotTranslation : TranslationEntity
{
    public Guid BallotId { get; set; }

    public Ballot? Ballot { get; set; }

    public string Description { get; set; } = string.Empty;
}
