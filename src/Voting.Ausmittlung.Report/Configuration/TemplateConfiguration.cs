// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Configuration;

public class TemplateConfiguration
{
    public List<TemplateWithFormattedValuesConfiguration> TemplatesWithFormattedValues { get; set; } = [];

    public bool ShouldUseLegacyNumberFormat(string templateKey, DomainOfInfluenceCanton canton)
    {
        return !TemplatesWithFormattedValues.Exists(x => x.TemplateKey == templateKey && x.Canton == canton);
    }
}
