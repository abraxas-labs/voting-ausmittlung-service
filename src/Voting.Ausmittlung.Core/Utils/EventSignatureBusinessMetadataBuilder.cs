// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1.Metadata;

namespace Voting.Ausmittlung.Core.Utils;

internal static class EventSignatureBusinessMetadataBuilder
{
    public static EventSignatureBusinessMetadata BuildFrom(Guid contestId) => new() { ContestId = contestId.ToString() };
}
