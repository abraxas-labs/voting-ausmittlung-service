// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionListUnionEndResult
{
    [XmlElement("HagenbachBischoffGroup")]
    public List<PdfHagenbachBischoffGroup> Groups { get; set; }
        = new List<PdfHagenbachBischoffGroup>();

    [XmlElement("ProportionalElectionListUnionEndResult")]
    public List<PdfProportionalElectionListUnionEndResultEntry> Entries { get; set; }
        = new List<PdfProportionalElectionListUnionEndResultEntry>();

    public void FormatNumbers(bool useLegacyFormat)
    {
        foreach (var group in Groups)
        {
            group.FormatNumbers(useLegacyFormat);
        }

        foreach (var entry in Entries)
        {
            entry.FormatNumbers(useLegacyFormat);
        }
    }
}
