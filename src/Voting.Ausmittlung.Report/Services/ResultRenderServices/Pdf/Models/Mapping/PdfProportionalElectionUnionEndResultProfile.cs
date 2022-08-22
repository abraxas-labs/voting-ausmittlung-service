// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfProportionalElectionUnionEndResultProfile : Profile
{
    public PdfProportionalElectionUnionEndResultProfile()
    {
        CreateMap<ProportionalElectionUnionEndResult, PdfProportionalElectionUnionEndResult>();
        CreateMap<ProportionalElectionUnionListEndResult, PdfProportionalElectionUnionListEndResult>();
    }
}
