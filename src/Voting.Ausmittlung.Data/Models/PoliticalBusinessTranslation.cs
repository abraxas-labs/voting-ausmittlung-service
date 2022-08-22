// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public abstract class PoliticalBusinessTranslation : TranslationEntity
{
    public string ShortDescription { get; set; } = string.Empty;

    public string OfficialDescription { get; set; } = string.Empty;
}
