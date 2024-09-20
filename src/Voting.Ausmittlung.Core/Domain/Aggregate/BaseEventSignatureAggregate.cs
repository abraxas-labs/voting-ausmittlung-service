// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Google.Protobuf;
using Voting.Ausmittlung.Core.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public abstract class BaseEventSignatureAggregate : BaseEventSourcingAggregate
{
    /// <summary>
    /// Raises an event and applies an event signature by the provided eventDataToSign if necessary.
    /// This stores the event in the uncommited events and immediately applies it to this aggregate.
    /// </summary>
    /// <param name="eventData">The event data of the event.</param>
    /// <param name="eventSignatureBusinessDomainData">The domain data to provide metadata informations.</param>
    protected void RaiseEvent(IMessage eventData, EventSignatureBusinessDomainData eventSignatureBusinessDomainData)
    {
        RaiseEvent(eventData, EventSignatureBusinessMetadataBuilder.BuildFrom(eventSignatureBusinessDomainData.ContestId));
    }
}
