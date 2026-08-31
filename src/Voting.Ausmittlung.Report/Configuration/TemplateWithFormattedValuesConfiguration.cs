// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Configuration;

/// <summary>
/// Defines that the specified template should send all values already pre-templated, e.g. percentages formatted as 97.23%.
/// </summary>
public class TemplateWithFormattedValuesConfiguration
{
    public string TemplateKey { get; set; } = string.Empty;

    public DomainOfInfluenceCanton Canton { get; set; }
}
