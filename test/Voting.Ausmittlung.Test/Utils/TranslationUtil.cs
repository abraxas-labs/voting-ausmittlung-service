// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Testing.Utils;

namespace Voting.Ausmittlung.Test.Utils;

public static class TranslationUtil
{
    public static List<T> CreateTranslations<T>(
        Action<T, string> field1Setter,
        string field1Value,
        Action<T, string>? field2Setter = null,
        string? field2Value = null,
        Action<T, string>? field3Setter = null,
        string? field3Value = null)
        where T : TranslationEntity, new()
    {
        var translations = new List<T>();

        foreach (var (lang, field1Content) in LanguageUtil.MockAllLanguages(field1Value))
        {
            var translation = new T { Language = lang };
            field1Setter(translation, field1Content);

            if (field2Setter != null)
            {
                var field2Content = $"{field2Value} {lang}";
                field2Setter(translation, field2Content);
            }

            if (field3Setter != null)
            {
                var field3Content = $"{field3Value} {lang}";
                field3Setter(translation, field3Content);
            }

            translations.Add(translation);
        }

        return translations;
    }
}
