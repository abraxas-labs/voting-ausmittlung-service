// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public sealed class ContestProfile : Profile
{
    public ContestProfile()
    {
        CreateMap<ContestEventData, Contest>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<ContestTranslation>(
                ((t, x) => t.Description = x, src.Description))));
    }
}
