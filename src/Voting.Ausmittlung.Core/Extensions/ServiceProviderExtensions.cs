// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Services;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Core.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates a new <see cref="IServiceScope"/> while keeping the <see cref="IAuth"/> and <see cref="LanguageService"/> from the existing service scope.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A new service scope with the copied authentication and language information.</returns>
    public static IServiceScope CreateScopeCopyAuthAndLanguage(this IServiceProvider serviceProvider)
    {
        var outerLanguageService = serviceProvider.GetRequiredService<LanguageService>();
        var outerAuth = serviceProvider.GetRequiredService<IAuth>();
        var scope = serviceProvider.CreateScope();
        try
        {
            var authStore = scope.ServiceProvider.GetRequiredService<IAuthStore>();
            authStore.SetValues(outerAuth.AccessToken, outerAuth.User, outerAuth.Tenant, outerAuth.Roles);

            var languageService = scope.ServiceProvider.GetRequiredService<LanguageService>();
            languageService.SetLanguage(outerLanguageService.Language);
            return scope;
        }
        catch (Exception)
        {
            scope.Dispose();
            throw;
        }
    }
}
