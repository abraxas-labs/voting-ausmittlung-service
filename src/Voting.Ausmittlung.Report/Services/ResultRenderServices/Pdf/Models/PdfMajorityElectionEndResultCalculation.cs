// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionEndResultCalculation
{
    private decimal? _absoluteMajorityThreshold;

    [XmlIgnore]
    public int? DecisiveVoteCount { get; set; }

    [XmlElement("DecisiveVoteCount")]
    public string? DecisiveVoteCountFormatted { get; set; }

    [XmlIgnore]
    public decimal? AbsoluteMajorityThreshold
    {
        get
        {
            return _absoluteMajorityThreshold;
        }

        set
        {
            _absoluteMajorityThreshold = value.HasValue
                ? decimal.Round(value.Value, 1, MidpointRounding.AwayFromZero)
                : null;
        }
    }

    [XmlElement("AbsoluteMajorityThreshold")]
    public string? AbsoluteMajorityThresholdFormatted { get; set; }

    [XmlIgnore]
    public int? AbsoluteMajority { get; set; }

    [XmlElement("AbsoluteMajority")]
    public string? AbsoluteMajorityFormatted { get; set; }

    public void FormatNumbers(bool useLegacyFormat)
    {
        if (DecisiveVoteCount.HasValue)
        {
            DecisiveVoteCountFormatted = NumberFormatUtil.FormatInteger(DecisiveVoteCount.Value, useLegacyFormat);
        }

        if (AbsoluteMajority.HasValue)
        {
            AbsoluteMajorityFormatted = NumberFormatUtil.FormatInteger(AbsoluteMajority.Value, useLegacyFormat);
        }

        if (AbsoluteMajorityThreshold.HasValue)
        {
            AbsoluteMajorityThresholdFormatted = NumberFormatUtil.FormatDecimal(AbsoluteMajorityThreshold.Value, useLegacyFormat, 1);
        }
    }
}
