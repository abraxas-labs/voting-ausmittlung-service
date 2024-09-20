// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using UtilContestDomainOfInfluenceDetails = Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models.ContestDomainOfInfluenceDetails;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfContestProfile : Profile
{
    public PdfContestProfile()
    {
        CreateMap<Contest, PdfContest>();

        CreateMap<ContestDetails, PdfContestDetails>();
        CreateMap<ContestDomainOfInfluenceDetails, PdfContestDomainOfInfluenceDetails>();
        CreateMap<UtilContestDomainOfInfluenceDetails, PdfContestDomainOfInfluenceDetails>();
        CreateMap<ContestCountingCircleDetails, PdfContestCountingCircleDetails>();

        CreateMap<ContestVotingCardResultDetail, PdfVotingCardResultDetail>();
        CreateMap<ContestCountOfVotersInformationSubTotal, PdfCountOfVotersInformationSubTotal>();
    }
}
