// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Ech0090_1_0;
using Voting.Lib.Common;
using Voting.Lib.Ech;

namespace Voting.Ausmittlung.Ech.Converters;

public class Ech0090Serializer
{
    private readonly IEchMessageIdProvider _messageIdProvider;
    private readonly IClock _clock;

    public Ech0090Serializer(IEchMessageIdProvider messageIdProvider, IClock clock)
    {
        _messageIdProvider = messageIdProvider;
        _clock = clock;
    }

    public EnvelopeType ToSedexEnvelope(int messageType, int messageClass, string senderId, List<string> recipientIds)
    {
        return new EnvelopeType
        {
            EventDate = _clock.UtcNow,
            MessageDate = _clock.UtcNow,
            MessageId = _messageIdProvider.NewId(),
            MessageType = messageType,
            MessageClass = messageClass,
            RecipientId = recipientIds.ConvertAll(r => r.Trim()),
            SenderId = senderId.Trim(),
            Version = EnvelopeTypeVersion.Item10,
        };
    }
}
