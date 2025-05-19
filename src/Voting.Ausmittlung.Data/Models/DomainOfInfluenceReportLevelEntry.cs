// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluenceReportLevelEntry
{
    public Guid Id { get; set; }

    [NotMapped]
    public DomainOfInfluence? DomainOfInfluence { get; set; }

    /// <summary>
    /// Gets or sets the report level of this domain of influence (relative to the political business DOI).
    /// </summary>
    public int ReportLevel { get; set; }
}
