// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class EventNotAppliedException : Exception
{
    public EventNotAppliedException(Type? eventType)
        : base(eventType == null ? "Event is null, was not applied" : $"{eventType} was not applied")
    {
    }
}
