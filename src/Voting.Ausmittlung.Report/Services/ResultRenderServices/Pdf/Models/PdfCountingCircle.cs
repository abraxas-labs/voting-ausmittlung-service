// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfCountingCircle
{
    [XmlIgnore]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NameForProtocol { get; set; } = string.Empty;

    public string Bfs { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int SortNumber { get; set; }

    public PdfAuthority? ResponsibleAuthority { get; set; }

    public PdfContactPerson? ContactPersonDuringEvent { get; set; }

    public bool? ContactPersonSameDuringEventAsAfter { get; set; }

    public PdfContactPerson? ContactPersonAfterEvent { get; set; }

    public PdfContestCountingCircleDetails? ContestCountingCircleDetails { get; set; }

    public bool ShouldSerializeContactPersonSameDuringEventAsAfter() => ContactPersonSameDuringEventAsAfter.HasValue;
}
