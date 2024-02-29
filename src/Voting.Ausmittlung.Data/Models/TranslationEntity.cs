// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class TranslationEntity : BaseEntity
{
    public string Language { get; set; } = string.Empty;
}
