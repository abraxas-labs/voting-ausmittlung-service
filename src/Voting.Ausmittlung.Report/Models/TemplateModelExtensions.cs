// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;
using Voting.Lib.VotingExports.Models;
using DomainOfInfluenceType = Voting.Lib.VotingExports.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Models;

internal static class TemplateModelExtensions
{
    internal static bool CanExport(this TemplateModel model, PoliticalBusinessType businessType, DomainOfInfluenceType doiType)
    {
        if (model.EntityType != EntityType.Contest && model.EntityType.ToPoliticalBusinessType() != businessType)
        {
            return false;
        }

        return model.MatchesDomainOfInfluenceType(doiType);
    }
}
