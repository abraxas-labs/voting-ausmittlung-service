// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionResultSubTotal
{
    [XmlIgnore]
    public int IndividualVoteCount { get; set; }

    [XmlElement("IndividualVoteCount")]
    public string IndividualVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EmptyVoteCountInclWriteIns { get; set; }

    [XmlElement("EmptyVoteCountInclWriteIns")]
    public string EmptyVoteCountInclWriteInsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int InvalidVoteCount { get; set; }

    [XmlElement("InvalidVoteCount")]
    public string InvalidVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalEmptyAndInvalidVoteCount { get; set; }

    [XmlElement("TotalEmptyAndInvalidVoteCount")]
    public string TotalEmptyAndInvalidVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCandidateVoteCountExclIndividual { get; set; }

    [XmlElement("TotalCandidateVoteCountExclIndividual")]
    public string TotalCandidateVoteCountExclIndividualFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCandidateVoteCountInclIndividual { get; set; }

    [XmlElement("TotalCandidateVoteCountInclIndividual")]
    public string TotalCandidateVoteCountInclIndividualFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalVoteCount { get; set; }

    [XmlElement("TotalVoteCount")]
    public string TotalVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EmptyVoteCountWriteIns { get; set; }

    [XmlElement("EmptyVoteCountWriteIns")]
    public string EmptyVoteCountWriteInsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EmptyVoteCountExclWriteIns { get; set; }

    [XmlElement("EmptyVoteCountExclWriteIns")]
    public string EmptyVoteCountExclWriteInsFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        IndividualVoteCountFormatted = NumberFormatUtil.FormatInteger(IndividualVoteCount, useLegacyFormat);
        EmptyVoteCountInclWriteInsFormatted = NumberFormatUtil.FormatInteger(EmptyVoteCountInclWriteIns, useLegacyFormat);
        InvalidVoteCountFormatted = NumberFormatUtil.FormatInteger(InvalidVoteCount, useLegacyFormat);
        TotalEmptyAndInvalidVoteCountFormatted = NumberFormatUtil.FormatInteger(TotalEmptyAndInvalidVoteCount, useLegacyFormat);
        TotalCandidateVoteCountExclIndividualFormatted = NumberFormatUtil.FormatInteger(TotalCandidateVoteCountExclIndividual, useLegacyFormat);
        TotalCandidateVoteCountInclIndividualFormatted = NumberFormatUtil.FormatInteger(TotalCandidateVoteCountInclIndividual, useLegacyFormat);
        TotalVoteCountFormatted = NumberFormatUtil.FormatInteger(TotalVoteCount, useLegacyFormat);
        EmptyVoteCountWriteInsFormatted = NumberFormatUtil.FormatInteger(EmptyVoteCountWriteIns, useLegacyFormat);
        EmptyVoteCountExclWriteInsFormatted = NumberFormatUtil.FormatInteger(EmptyVoteCountExclWriteIns, useLegacyFormat);
    }
}
