// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfCountingCircleProfile : Profile
{
    public PdfCountingCircleProfile()
    {
        CreateMap<CountingCircle, PdfCountingCircle>();
        CreateMap<VotingCardResultDetail, PdfVotingCardResultDetail>();
        CreateMap<CountOfVotersInformationSubTotal, PdfCountOfVotersInformationSubTotal>();
    }
}
