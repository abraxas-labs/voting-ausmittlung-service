// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.EventSignature;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventSignature(this IServiceCollection services)
    {
        return services
            .AddSingleton<PublicKeySignatureVerifier>()
            .AddForwardRefSingleton<IContestKeyDataProvider, ContestCache>();
    }
}
