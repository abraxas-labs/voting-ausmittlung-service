// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Ech.Mapping;

public static class LanguageMapping
{
    internal static Dictionary<string, string> FilterEchExportLanguages(this Dictionary<string, string> translations, bool eVoting)
    {
        return eVoting
            ? translations
            : translations.Where(t => t.Key == Languages.German).ToDictionary();
    }

    internal static IReadOnlyCollection<string> GetEchExportLanguages(bool eVoting)
    {
        return eVoting
            ? Languages.All.ToList()
            : new List<string> { Languages.German };
    }

    internal static ICollection<TPoliticalBusinessTranslation> FilterAndSortEchExportLanguages<TPoliticalBusinessTranslation>(
        this ICollection<TPoliticalBusinessTranslation> translations,
        bool eVoting)
        where TPoliticalBusinessTranslation : TranslationEntity
    {
        return eVoting
            ? translations.OrderBy(t => t.Language).ToList()
            : translations.Where(t => t.Language == Languages.German).ToList();
    }
}
