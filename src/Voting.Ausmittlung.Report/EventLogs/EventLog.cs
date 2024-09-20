// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Google.Protobuf;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs;

public class EventLog
{
    public string EventFullName { get; set; } = string.Empty;

    public IMessage? EventContent { get; set; }

    public DateTime Timestamp { get; set; }

    public Contest? Contest { get; set; }

    public EventLogUser? EventUser { get; set; }

    public EventLogTenant? EventTenant { get; set; }

    public Guid? CountingCircleId { get; set; }

    public CountingCircle? CountingCircle { get; set; }

    public PoliticalBusinessType? PoliticalBusinessType { get; set; }

    public Guid? PoliticalBusinessId { get; set; }

    public string? PoliticalBusinessNumber { get; set; }

    public int? BundleNumber { get; set; }

    public int? BundleBallotNumber { get; set; }

    public List<EventLogTranslation> Translations { get; set; } = new();

    public EventLogEventSignatureVerification? EventSignatureVerification { get; set; }

    public EventLogPublicKeyData? PublicKeyData { get; set; }
}
