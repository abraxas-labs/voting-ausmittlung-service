// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfContest
{
    public DateTime Date { get; set; }

    public string Description { get; set; } = string.Empty;

    public PdfDomainOfInfluence? DomainOfInfluence { get; set; }

    [XmlElement("ContestDetails")]
    public PdfContestDetails? Details { get; set; }

    public ContestState State { get; set; }
}
