// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfHagenbachBischoffGroup : PdfHagenbachBischoffSimpleGroup
{
    /// <summary>
    /// Gets or sets the number of mandates gained from the initial distribution.
    /// </summary>
    public int InitialNumberOfMandates { get; set; }

    /// <summary>
    /// Gets or sets the number of mandates of the latest calculation round.
    /// </summary>
    public int NumberOfMandates { get; set; }

    // setter is needed due to xml serializer limitation
    public int NumberOfMandatesPlusOne
    {
        get => NumberOfMandates + 1;
        set => throw new InvalidOperationException("readonly property");
    }

    /// <summary>
    /// Gets or sets the quotient.
    /// </summary>
    public decimal Quotient { get; set; }

    /// <summary>
    /// Gets or sets the quotient rounded to the next integer.
    /// </summary>
    public int DistributionNumber { get; set; }

    [XmlElement("HagenbachBischoffInitialDistribution")]
    public List<PdfHagenbachBischoffInitialGroupValues> InitialDistributionValues { get; set; }
        = new List<PdfHagenbachBischoffInitialGroupValues>();

    [XmlElement("HagenbachBischoffCalculationRound")]
    public List<PdfHagenbachBischoffCalculationRound> CalculationRounds { get; set; }
        = new List<PdfHagenbachBischoffCalculationRound>();

    public int SumInitialDistributionNumberOfMandates
    {
        get => InitialDistributionValues.Count > 0
            ? InitialDistributionValues.Sum(x => x.NumberOfMandates)
            : 0;
        set => throw new InvalidOperationException("readonly property");
    }
}
