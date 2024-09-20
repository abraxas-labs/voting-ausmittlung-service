// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Google.Protobuf;
using Voting.Lib.Eventing.Subscribe;

namespace Voting.Ausmittlung.Core.EventProcessors;

public interface IEventProcessor<TEvent> : IEventProcessor<EventProcessorScope, TEvent>
    where TEvent : IMessage<TEvent>
{
}
