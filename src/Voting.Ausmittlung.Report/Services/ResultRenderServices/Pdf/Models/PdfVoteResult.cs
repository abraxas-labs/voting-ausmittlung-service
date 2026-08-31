// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteResult : PdfCountingCircleResult
{
    [XmlElement("BallotResult")]
    public List<PdfBallotResult> Results { get; set; } = new List<PdfBallotResult>();

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);

        foreach (var result in Results)
        {
            result.FormatNumbers(useLegacyFormat);
        }
    }
}
