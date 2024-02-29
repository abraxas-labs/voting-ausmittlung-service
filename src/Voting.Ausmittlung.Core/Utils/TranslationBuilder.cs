// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils;

public static class TranslationBuilder
{
    public static List<T> CreateTranslations<T>(params (Action<T, string> FieldSetter, IDictionary<string, string> FieldValues)[] fields)
        where T : TranslationEntity, new()
    {
        var translations = new List<T>();
        var languages = fields
            .Where(f => f.FieldValues != null)
            .SelectMany(f => f.FieldValues.Select(v => v.Key))
            .Distinct()
            .ToList();

        foreach (var lang in languages)
        {
            var translation = new T { Language = lang };

            foreach (var (fieldSetter, fieldValues) in fields)
            {
                if (fieldValues.TryGetValue(lang, out var fieldContent))
                {
                    fieldSetter(translation, fieldContent);
                }
            }

            translations.Add(translation);
        }

        return translations;
    }
}
