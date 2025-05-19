// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

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

    public int PreviousNumberOfMandatesPlusOne
    {
        get => PreviousNumberOfMandates + 1;
        set
        {
            // only needed due to xml serializer
        }
    }

    public bool IsWinner { get; set; }
}
