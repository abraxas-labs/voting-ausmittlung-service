// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Extensions;

public static class TranslationExtensions
{
    /// <summary>
    /// Gets the translated field.
    /// Note that the query should use the default query filters, which should automatically retrieve the correct translation.
    /// </summary>
    /// <param name="translations">The list of translations for the field.</param>
    /// <param name="fieldFunc">The function to retrieve the field.</param>
    /// <param name="optional">Whether the translated field is optional.</param>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <returns>The translated field value or an empty string if no translation exist and the translation is optional.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the translations aren't loaded correctly.</exception>
    public static string GetTranslated<T>(this IEnumerable<T> translations, Func<T, string> fieldFunc, bool optional = false)
    {
        // The default query filter should automatically retrieve only one or zero translations in the correct language
        if (translations.Count() > 1)
        {
            throw new InvalidOperationException("Multiple translations available. Correct result cannot be guaranteed");
        }

        var translation = translations.FirstOrDefault();

        if (translation == null)
        {
            return optional
                ? string.Empty
                : throw new InvalidOperationException("Translations not loaded or not available for the current language");
        }

        return fieldFunc(translation);
    }
}
