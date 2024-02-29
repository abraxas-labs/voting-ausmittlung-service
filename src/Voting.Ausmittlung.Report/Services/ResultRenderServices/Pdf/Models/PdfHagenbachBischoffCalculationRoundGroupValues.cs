// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfHagenbachBischoffCalculationRoundGroupValues
{
    [XmlElement("HagenbachBischoffGroup")]
    public PdfHagenbachBischoffSimpleGroup? Group { get; set; }

    public decimal NextQuotient { get; set; }

    public decimal PreviousQuotient { get; set; }

    public int NumberOfMandates { get; set; }

    public int PreviousNumberOfMandates { get; set; }

    // setter is needed due to xml serializer limitation
    public int PreviousNumberOfMandatesPlusOne
    {
        get => PreviousNumberOfMandates + 1;
        set => throw new InvalidOperationException("readonly property");
    }

    public bool IsWinner { get; set; }
}
