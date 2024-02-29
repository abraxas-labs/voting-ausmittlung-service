// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public sealed class DomainOfInfluenceProfile : Profile
{
    public DomainOfInfluenceProfile()
    {
        CreateMap<DomainOfInfluenceEventData, DomainOfInfluence>()
            .ForMember(dst => dst.BasisDomainOfInfluenceId, opts => opts.MapFrom(src => src.Id))
            .ForMember(dst => dst.ContactPerson, opts => opts.NullSubstitute(new ContactPersonEventData()));
        CreateMap<DomainOfInfluencePartyEventData, DomainOfInfluenceParty>()
            .ForMember(dst => dst.BaseDomainOfInfluencePartyId, opts => opts.MapFrom(src => src.Id))
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<DomainOfInfluencePartyTranslation>(
                ((t, x) => t.Name = x, src.Name),
                ((t, x) => t.ShortDescription = x, src.ShortDescription))));
        CreateMap<DomainOfInfluenceCantonDefaults, DomainOfInfluenceCantonDefaults>();
        CreateMap<DomainOfInfluenceCantonDefaultsVotingCardChannel, DomainOfInfluenceCantonDefaultsVotingCardChannel>();
    }
}
