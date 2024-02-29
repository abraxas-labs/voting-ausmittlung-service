// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Configuration;

public class EventSignatureConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether signing events is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum count of attempts to publish a signature event.
    /// Signature events are published on a stream per contest.
    /// Each service instance in the Publisher mode tries to publish a signature event for the same contest at roughly the same time,
    /// which can lead to concurrency exceptions.
    /// In that case, the event publishing should be retried.
    /// Even if this count is exceeded, the action may be retried by the transient subscription.
    /// </summary>
    public int EventWritesMaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the max delay in milliseconds (exclusive) for a signature event publish retry.
    /// If a signature event write gets retried, the service instance waits a random number of milliseconds,
    /// between <see cref="EventWritesMaxAttempts"/> and <see cref="EventWritesMaxAttempts"/> to retry.
    /// If all service instances would retry at the same delay, the concurrency problem may consist.
    /// <seealso cref="EventWritesMaxAttempts"/>
    /// </summary>
    public int EventWritesRetryMaxDelayMillis { get; set; } = 100;

    /// <summary>
    /// Gets or sets the min delay in milliseconds (inclusive) for a signature event publish retry.
    /// <seealso cref="EventWritesRetryMaxDelayMillis"/>.
    /// </summary>
    public int EventWritesRetryMinDelayMillis { get; set; } = 0;
}
