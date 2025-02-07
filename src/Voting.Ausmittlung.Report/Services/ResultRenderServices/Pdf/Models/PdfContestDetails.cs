// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfContestDetails : PdfBaseDetails
{
    // This is a hack and only used for end results with only one counting circle (mostly communal results).
    public CountingMachine? CountingMachine { get; set; }

    [XmlIgnore]
    public bool CountingMachineSpecified => CountingMachine != null;
}
