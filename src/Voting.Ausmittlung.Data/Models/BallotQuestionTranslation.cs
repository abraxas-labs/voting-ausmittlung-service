// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class BallotQuestionTranslation : TranslationEntity
{
    public Guid BallotQuestionId { get; set; }

    public BallotQuestion? BallotQuestion { get; set; }

    public string Question { get; set; } = string.Empty;
}
