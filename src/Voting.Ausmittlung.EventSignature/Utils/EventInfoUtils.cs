// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Reflection;
using Google.Protobuf;
using Proto = Abraxas.Voting.Ausmittlung.Events.V1.Data;
using ProtoBasis = Abraxas.Voting.Basis.Events.V1.Data;

namespace Voting.Ausmittlung.EventSignature.Utils;

public static class EventInfoUtils
{
    public static PropertyInfo GetEventInfoPropertyInfo(IMessage message)
    {
        return message.GetType().GetProperty(nameof(Proto.EventInfo))
            ?? throw new ArgumentException("Event has no EventInfo field", nameof(message));
    }

    public static Proto.EventInfo MapEventInfo(object? eventInfo)
    {
        return eventInfo switch
        {
            Proto.EventInfo i => i,
            ProtoBasis.EventInfo basisInfo => new Proto.EventInfo // Some events are logged from VOTING Basis, which we map here to the Ausmittlung event info type
            {
                Tenant = new()
                {
                    Id = basisInfo.Tenant.Id,
                    Name = basisInfo.Tenant.Name,
                },
                User = new()
                {
                    Id = basisInfo.User.Id,
                    FirstName = basisInfo.User.FirstName,
                    LastName = basisInfo.User.LastName,
                    Username = basisInfo.User.Username,
                },
                Timestamp = basisInfo.Timestamp,
            },
            _ => throw new ArgumentException("Could not retrieve event info value", nameof(eventInfo)),
        };
    }

    public static Proto.EventInfo GetEventInfo(IMessage message)
    {
        var eventInfoProp = GetEventInfoPropertyInfo(message);
        var eventInfoObj = eventInfoProp.GetValue(message);
        return MapEventInfo(eventInfoObj);
    }
}
