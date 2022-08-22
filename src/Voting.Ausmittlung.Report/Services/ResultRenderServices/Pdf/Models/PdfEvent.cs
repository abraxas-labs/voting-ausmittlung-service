// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfEvent
{
    public DateTime Date { get; set; }

    [XmlElement("EventAttribute")]
    public List<PdfEventAttribute>? EventData { get; set; }

    public string EventName { get; set; } = string.Empty;

    public PdfEventTenant? Tenant { get; set; }

    public PdfEventUser? User { get; set; }

    public PdfEventCountingCircle? CountingCircle { get; set; }

    public PdfEventPoliticalBusiness? PoliticalBusiness { get; set; }

    public int? BundleNumber { get; set; }

    public int? BundleBallotNumber { get; set; }

    public PdfEventSignatureVerification EventSignatureVerification { get; set; }

    public bool ShouldSerializeBundleNumber() => BundleNumber.HasValue;

    public bool ShouldSerializeBundleBallotNumber() => BundleBallotNumber.HasValue;
}
