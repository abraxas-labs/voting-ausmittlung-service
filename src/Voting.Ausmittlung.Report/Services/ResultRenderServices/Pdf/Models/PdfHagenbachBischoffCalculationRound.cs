// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfHagenbachBischoffCalculationRound
{
    public HagenbachBischoffCalculationRoundWinnerReason WinnerReason { get; set; }

    public PdfHagenbachBischoffSimpleWinnerGroup? Winner { get; set; }

    [XmlElement("HagenbachBischoffGroupDetail")]
    public List<PdfHagenbachBischoffCalculationRoundGroupValues> GroupValues { get; set; }
        = new List<PdfHagenbachBischoffCalculationRoundGroupValues>();
}
