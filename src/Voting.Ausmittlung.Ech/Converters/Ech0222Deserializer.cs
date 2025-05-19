// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Ech.Models;

namespace Voting.Ausmittlung.Ech.Converters;

public class Ech0222Deserializer
{
    private readonly IServiceProvider _serviceProvider;

    public Ech0222Deserializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public VotingImport DeserializeXml(Ech0222Version version, Stream data)
        => _serviceProvider.GetRequiredKeyedService<IEch0222Deserializer>(version).DeserializeXml(data);
}
