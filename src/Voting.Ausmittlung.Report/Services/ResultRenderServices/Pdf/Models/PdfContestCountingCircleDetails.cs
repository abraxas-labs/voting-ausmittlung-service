// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfContestCountingCircleDetails : PdfBaseDetails
{
    [XmlIgnore]
    public Guid CountingCircleId { get; set; }
}
