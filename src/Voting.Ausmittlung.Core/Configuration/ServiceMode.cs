// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Configuration;

[Flags]
public enum ServiceMode
{
    /// <summary>
    /// The app acts as publisher (reader and writer) but does not update the database read model or process events.
    /// </summary>
    Publisher = 1 << 0,

    /// <summary>
    /// The app acts as event processor but does not expose endpoints (except for monitoring endpoints such as health and metrics).
    /// </summary>
    EventProcessor = 1 << 1,

    /// <summary>
    /// The app acts as <see cref="Publisher"/> and <see cref="EventProcessor"/>. Used mainly for development.
    /// </summary>
    Hybrid = Publisher | EventProcessor,
}
