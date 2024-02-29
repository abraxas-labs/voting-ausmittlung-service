// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class MultiLanguageTranslationUtil
{
    private readonly DataContext _dataContext;

    public MultiLanguageTranslationUtil(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public string GetShortDescription(PoliticalBusiness pb)
    {
        var language = _dataContext.Language
            ?? throw new InvalidOperationException("Language not set.");

        var translations = pb.PoliticalBusinessTranslations;
        var matchingTranslation = translations.FirstOrDefault(t => t.Language == language);

        return matchingTranslation?.ShortDescription
            ?? throw new InvalidOperationException("Translations not loaded or not available for the current language");
    }
}
