// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfHagenbachBischoffGroup : PdfHagenbachBischoffSimpleGroup
{
    /// <summary>
    /// Gets or sets the number of mandates gained from the initial distribution.
    /// </summary>
    [XmlIgnore]
    public int InitialNumberOfMandates { get; set; }

    [XmlElement("InitialNumberOfMandates")]
    public string InitialNumberOfMandatesFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of mandates of the latest calculation round.
    /// </summary>
    [XmlIgnore]
    public int NumberOfMandates { get; set; }

    [XmlElement("NumberOfMandates")]
    public string NumberOfMandatesFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int NumberOfMandatesPlusOne
    {
        get => NumberOfMandates + 1;
        set
        {
            // only needed due to xml serializer
        }
    }

    [XmlElement("NumberOfMandatesPlusOne")]
    public string NumberOfMandatesPlusOneFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quotient.
    /// </summary>
    [XmlIgnore]
    public decimal Quotient { get; set; }

    [XmlElement("Quotient")]
    public string QuotientFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quotient rounded to the next integer.
    /// </summary>
    [XmlIgnore]
    public int DistributionNumber { get; set; }

    [XmlElement("DistributionNumber")]
    public string DistributionNumberFormatted { get; set; } = string.Empty;

    [XmlElement("HagenbachBischoffInitialDistribution")]
    public List<PdfHagenbachBischoffInitialGroupValues> InitialDistributionValues { get; set; }
        = new List<PdfHagenbachBischoffInitialGroupValues>();

    [XmlElement("HagenbachBischoffCalculationRound")]
    public List<PdfHagenbachBischoffCalculationRound> CalculationRounds { get; set; }
        = new List<PdfHagenbachBischoffCalculationRound>();

    [XmlIgnore]
    public int SumInitialDistributionNumberOfMandates
    {
        get => InitialDistributionValues.Count > 0
            ? InitialDistributionValues.Sum(x => x.NumberOfMandates)
            : 0;
        set
        {
            // only needed due to xml serializer
        }
    }

    [XmlElement("SumInitialDistributionNumberOfMandates")]
    public string SumInitialDistributionNumberOfMandatesFormatted { get; set; } = string.Empty;

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);
        InitialNumberOfMandatesFormatted = NumberFormatUtil.FormatInteger(InitialNumberOfMandates, useLegacyFormat);
        NumberOfMandatesFormatted = NumberFormatUtil.FormatInteger(NumberOfMandates, useLegacyFormat);
        NumberOfMandatesPlusOneFormatted = NumberFormatUtil.FormatInteger(NumberOfMandatesPlusOne, useLegacyFormat);
        QuotientFormatted = NumberFormatUtil.FormatDecimal(Quotient, useLegacyFormat, 3);
        DistributionNumberFormatted = NumberFormatUtil.FormatInteger(DistributionNumber, useLegacyFormat);
        SumInitialDistributionNumberOfMandatesFormatted = NumberFormatUtil.FormatInteger(SumInitialDistributionNumberOfMandates, useLegacyFormat);

        foreach (var initialDistributionValues in InitialDistributionValues)
        {
            initialDistributionValues.FormatNumbers(useLegacyFormat);
        }

        foreach (var calculationRound in CalculationRounds)
        {
            calculationRound.FormatNumbers(useLegacyFormat);
        }
    }
}
