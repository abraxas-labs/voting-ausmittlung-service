// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Google.Protobuf.WellKnownTypes;
using Voting.Lib.Common;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Core.Utils;

public class EventInfoProvider
{
    private readonly IAuth _auth;
    private readonly IClock _clock;

    public EventInfoProvider(IAuth auth, IClock clock)
    {
        _auth = auth;
        _clock = clock;
    }

    public EventInfo NewEventInfo() => new EventInfo
    {
        Timestamp = _clock.UtcNow.ToTimestamp(),
        User = new()
        {
            Id = _auth.User.Loginid,
            FirstName = _auth.User.Firstname ?? string.Empty,
            LastName = _auth.User.Lastname ?? string.Empty,
            Username = _auth.User.Username ?? string.Empty,
        },
        Tenant = new()
        {
            Id = _auth.Tenant.Id,
            Name = _auth.Tenant.Name ?? string.Empty,
        },
    };
}
