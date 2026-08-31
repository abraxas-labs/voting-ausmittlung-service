// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteCountingCircleBallotResult : PdfCountingCircleResult
{
    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

    [XmlElement("BallotQuestionResult")]
    public List<PdfBallotQuestionResult>? QuestionResults { get; set; }

    [XmlElement("TieBreakQuestionResult")]
    public List<PdfTieBreakQuestionResult>? TieBreakQuestionResults { get; set; }

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);

        CountOfVoters?.FormatNumbers(useLegacyFormat);

        foreach (var questionResult in QuestionResults ?? [])
        {
            questionResult.FormatNumbers(useLegacyFormat);
        }

        foreach (var tieBreakResult in TieBreakQuestionResults ?? [])
        {
            tieBreakResult.FormatNumbers(useLegacyFormat);
        }
    }
}
