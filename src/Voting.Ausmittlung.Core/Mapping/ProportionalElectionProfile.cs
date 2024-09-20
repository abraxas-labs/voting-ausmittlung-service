// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class ProportionalElectionProfile : Profile
{
    public ProportionalElectionProfile()
    {
        CreateMap<ProportionalElectionEventData, ProportionalElection>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<ProportionalElectionTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.OfficialDescription = x, src.OfficialDescription))));
        CreateMap<ProportionalElectionAfterTestingPhaseUpdated, ProportionalElection>(MemberList.Source)
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<ProportionalElectionTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.OfficialDescription = x, src.OfficialDescription))))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<ProportionalElection, SimplePoliticalBusiness>()
            .ForMember(dst => dst.PoliticalBusinessTranslations, opts => opts.Ignore())
            .ForMember(dst => dst.CountingCircleResults, opts => opts.Ignore())
            .ForMember(dst => dst.SwissAbroadVotingRight, opts => opts.Ignore())
            .ForMember(dst => dst.PoliticalBusinessSubType, opts => opts.MapFrom(src => src.BusinessSubType))
            .AfterMap((_, dst) => dst.PoliticalBusinessType = PoliticalBusinessType.ProportionalElection);
        CreateMap<ProportionalElectionTranslation, SimplePoliticalBusinessTranslation>()
            .ForMember(dst => dst.Id, opts => opts.Ignore());

        CreateMap<ProportionalElectionListEventData, ProportionalElectionList>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<ProportionalElectionListTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.Description = x, src.Description))));
        CreateMap<ProportionalElectionListAfterTestingPhaseUpdated, ProportionalElectionList>(MemberList.Source)
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<ProportionalElectionListTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.Description = x, src.Description))))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<ProportionalElectionListUnionEventData, ProportionalElectionListUnion>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<ProportionalElectionListUnionTranslation>(
                ((t, x) => t.Description = x, src.Description))));

        CreateMap<ProportionalElectionCandidateEventData, ProportionalElectionCandidate>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<ProportionalElectionCandidateTranslation>(
                ((t, x) => t.Occupation = x, src.Occupation),
                ((t, x) => t.OccupationTitle = x, src.OccupationTitle))));
        CreateMap<ProportionalElectionCandidateAfterTestingPhaseUpdated, ProportionalElectionCandidate>(MemberList.Source)
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<ProportionalElectionCandidateTranslation>(
                ((t, x) => t.Occupation = x, src.Occupation),
                ((t, x) => t.OccupationTitle = x, src.OccupationTitle))))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());
    }
}
