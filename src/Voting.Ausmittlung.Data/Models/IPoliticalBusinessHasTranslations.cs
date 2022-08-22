// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public interface IPoliticalBusinessHasTranslations
{
    IEnumerable<PoliticalBusinessTranslation> Translations { get; set; }
}
