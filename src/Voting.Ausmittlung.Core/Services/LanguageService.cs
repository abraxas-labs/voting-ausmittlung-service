// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.Services;

public class LanguageService
{
    private const string FallbackLanguage = Languages.German;
    private readonly ILogger<LanguageService> _logger;
    private readonly DataContext _dataContext;

    public LanguageService(ILogger<LanguageService> logger, DataContext dataContext)
    {
        _logger = logger;
        _dataContext = dataContext;
    }

    /// <summary>
    /// Gets the current language or the fallback language if none is set.
    /// </summary>
    public string Language => _dataContext.Language ?? FallbackLanguage;

    /// <summary>
    /// Sets the language used for subsequent operations.
    /// </summary>
    /// <param name="lang">The language to use.</param>
    public void SetLanguage(string? lang)
    {
        if (!Languages.All.Contains(lang))
        {
            _logger.LogWarning("Language {lang} is not valid, using fallback language", lang);
            lang = FallbackLanguage;
        }
        else
        {
            _logger.LogDebug("Using language {lang} for this request", lang);
        }

        _dataContext.Language = lang;
    }
}
