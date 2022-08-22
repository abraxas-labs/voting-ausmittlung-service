// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionEndResultCalculation
{
    [XmlElement("HagenbachBischoffRootGroup")]
    public PdfHagenbachBischoffGroup? HagenbachBischoffRootGroup { get; set; }

    [XmlElement("HagenbachBischoffListUnionGroup")]
    public List<PdfHagenbachBischoffGroup> HagenbachBischoffListUnionGroups { get; set; }
        = new List<PdfHagenbachBischoffGroup>();

    [XmlElement("HagenbachBischoffSubListUnionGroup")]
    public List<PdfHagenbachBischoffGroup> HagenbachBischoffSubListUnionGroups { get; set; }
        = new List<PdfHagenbachBischoffGroup>();
}
