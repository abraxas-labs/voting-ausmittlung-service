// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfContestDomainOfInfluenceDetails : PdfBaseDetails
{
    [XmlIgnore]
    public Guid DomainOfInfluenceId { get; set; }
}
