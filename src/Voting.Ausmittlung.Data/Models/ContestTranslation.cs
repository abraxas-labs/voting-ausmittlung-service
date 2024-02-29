// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ContestTranslation : TranslationEntity
{
    public Guid ContestId { get; set; }

    public Contest? Contest { get; set; }

    public string Description { get; set; } = string.Empty;
}
