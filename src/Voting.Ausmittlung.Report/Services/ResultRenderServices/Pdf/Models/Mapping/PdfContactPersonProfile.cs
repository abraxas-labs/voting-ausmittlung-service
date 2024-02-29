// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfContactPersonProfile : Profile
{
    public PdfContactPersonProfile()
    {
        CreateMap<CountingCircleContactPerson, PdfContactPerson>();
    }
}
