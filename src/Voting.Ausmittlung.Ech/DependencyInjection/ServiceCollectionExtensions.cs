// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Ech.Configuration;

namespace Voting.Ausmittlung.Ech.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEch(this IServiceCollection services, EchConfig config)
        => services
            .AddVotingLibEch(config)
            .AddSingleton<Ech0110Serializer>()
            .AddSingleton<Ech0222Serializer>()
            .AddSingleton<Ech0110Deserializer>()
            .AddKeyedSingleton<IEch0222Deserializer, Ech0222_1_0_Deserializer>(Ech0222Version.V1)
            .AddKeyedSingleton<IEch0222Deserializer, Ech0222_3_0_Deserializer>(Ech0222Version.V3)
            .AddSingleton<Ech0222Deserializer>()
            .AddSingleton<Ech0252Serializer>();
}
