// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

public class PoliticalBusinessProfile : Profile
{
    public PoliticalBusinessProfile()
    {
        CreateMap<ElectionResultEntryParams, DomainModels.ElectionResultEntryParams>();
    }
}
