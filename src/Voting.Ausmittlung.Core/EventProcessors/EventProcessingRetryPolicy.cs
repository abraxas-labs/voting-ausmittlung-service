// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Voting.Lib.Eventing.Subscribe;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class EventProcessingRetryPolicy : EventProcessingRetryPolicy<EventProcessorScope>
{
    private readonly EventProcessingInMemoryStateHolder _stateHolder;

    public EventProcessingRetryPolicy(ILogger<EventProcessingRetryPolicy> logger, EventProcessingInMemoryStateHolder stateHolder)
        : base(logger)
    {
        _stateHolder = stateHolder;
    }

    public override Task<bool> Failed(SubscriptionDroppedReason reason)
    {
        _stateHolder.ResetState();
        return base.Failed(reason);
    }
}
