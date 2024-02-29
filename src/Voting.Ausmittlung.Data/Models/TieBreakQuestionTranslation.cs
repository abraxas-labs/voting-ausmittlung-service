// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class TieBreakQuestionTranslation : TranslationEntity
{
    public Guid TieBreakQuestionId { get; set; }

    public TieBreakQuestion? TieBreakQuestion { get; set; }

    public string Question { get; set; } = string.Empty;
}
