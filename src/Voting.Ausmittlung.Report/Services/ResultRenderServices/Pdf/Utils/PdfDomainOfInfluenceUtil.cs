// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Resources;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

internal static class PdfDomainOfInfluenceUtil
{
    internal static string MapDomainOfInfluenceType(DomainOfInfluenceType doiType)
    {
        return doiType switch
        {
            DomainOfInfluenceType.Ch => Strings.Exports_DomainOfInfluenceType_Ch,
            DomainOfInfluenceType.Ct => Strings.Exports_DomainOfInfluenceType_Ct,
            DomainOfInfluenceType.Bz => Strings.Exports_DomainOfInfluenceType_Ct,
            DomainOfInfluenceType.Mu => Strings.Exports_DomainOfInfluenceType_Mu,
            DomainOfInfluenceType.Sk => Strings.Exports_DomainOfInfluenceType_Mu,
            _ => Strings.Exports_DomainOfInfluenceType_Other,
        };
    }
}
