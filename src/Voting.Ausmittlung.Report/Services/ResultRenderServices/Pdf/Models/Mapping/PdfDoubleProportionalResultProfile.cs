// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfDoubleProportionalResultProfile : Profile
{
    public PdfDoubleProportionalResultProfile()
    {
        CreateMap<DoubleProportionalResult, PdfDoubleProportionalResult>();
        CreateMap<DoubleProportionalResultRow, PdfDoubleProportionalResultRow>();
        CreateMap<DoubleProportionalResultColumn, PdfDoubleProportionalResultColumn>();
        CreateMap<DoubleProportionalResultCell, PdfDoubleProportionalResultCell>();
    }
}
