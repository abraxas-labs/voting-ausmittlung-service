// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Concurrent;
using Google.Protobuf.Reflection;
using Voting.Ausmittlung.Report.EventLogs.EventProcessors;

namespace Voting.Ausmittlung.Report.EventLogs;

public class EventLogInitializerAdapterRegistry
{
    private readonly ConcurrentDictionary<string, Type?> _processorTypes = new();
    private readonly IServiceProvider _serviceProvider;

    public EventLogInitializerAdapterRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    internal IReportEventProcessorAdapter? GetInitializerAdapter(MessageDescriptor descriptor)
    {
        var processorType = _processorTypes.GetOrAdd(descriptor.FullName, typeof(ReportEventProcessorAdapter<>).MakeGenericType(descriptor.ClrType));
        return processorType == null
            ? null
            : (IReportEventProcessorAdapter?)_serviceProvider?.GetService(processorType);
    }
}
