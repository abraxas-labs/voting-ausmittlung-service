// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class MajorityElectionProfile : Profile
{
    public MajorityElectionProfile()
    {
        CreateMap<MajorityElectionEventData, MajorityElection>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<MajorityElectionTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.OfficialDescription = x, src.OfficialDescription))));
        CreateMap<MajorityElectionAfterTestingPhaseUpdated, MajorityElection>(MemberList.Source)
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<MajorityElectionTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.OfficialDescription = x, src.OfficialDescription))))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<MajorityElection, SimplePoliticalBusiness>()
            .ForMember(dst => dst.PoliticalBusinessTranslations, opts => opts.Ignore())
            .ForMember(dst => dst.CountingCircleResults, opts => opts.Ignore())
            .ForMember(dst => dst.SwissAbroadVotingRight, opts => opts.Ignore())
            .AfterMap((_, dst) => dst.PoliticalBusinessType = PoliticalBusinessType.MajorityElection);
        CreateMap<MajorityElectionTranslation, SimplePoliticalBusinessTranslation>()
            .ForMember(dst => dst.Id, opts => opts.Ignore());

        CreateMap<MajorityElectionCandidateEventData, MajorityElectionCandidate>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<MajorityElectionCandidateTranslation>(
                ((t, x) => t.Occupation = x, src.Occupation),
                ((t, x) => t.OccupationTitle = x, src.OccupationTitle),
                ((t, x) => t.Party = x, src.Party))));
        CreateMap<MajorityElectionCandidateAfterTestingPhaseUpdated, MajorityElectionCandidate>(MemberList.Source)
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<MajorityElectionCandidateTranslation>(
                ((t, x) => t.Occupation = x, src.Occupation),
                ((t, x) => t.OccupationTitle = x, src.OccupationTitle),
                ((t, x) => t.Party = x, src.Party))))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<SecondaryMajorityElectionEventData, SecondaryMajorityElection>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<SecondaryMajorityElectionTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.OfficialDescription = x, src.OfficialDescription))));
        CreateMap<SecondaryMajorityElectionAfterTestingPhaseUpdated, SecondaryMajorityElection>(MemberList.Source)
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<SecondaryMajorityElectionTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.OfficialDescription = x, src.OfficialDescription))))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<SecondaryMajorityElection, SimplePoliticalBusiness>()
            .ForMember(dst => dst.PoliticalBusinessTranslations, opts => opts.Ignore())
            .ForMember(dst => dst.CountingCircleResults, opts => opts.Ignore())
            .ForMember(dst => dst.SwissAbroadVotingRight, opts => opts.Ignore())
            .AfterMap((_, dst) => dst.PoliticalBusinessType = PoliticalBusinessType.SecondaryMajorityElection);
        CreateMap<SecondaryMajorityElectionTranslation, SimplePoliticalBusinessTranslation>()
            .ForMember(dst => dst.Id, opts => opts.Ignore());

        CreateMap<MajorityElectionCandidateEventData, SecondaryMajorityElectionCandidate>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                ((t, x) => t.Occupation = x, src.Occupation),
                ((t, x) => t.OccupationTitle = x, src.OccupationTitle),
                ((t, x) => t.Party = x, src.Party))))
            .ForMember(dst => dst.SecondaryMajorityElectionId, opts => opts.MapFrom(src => src.MajorityElectionId));

        CreateMap<MajorityElectionCandidateReferenceEventData, SecondaryMajorityElectionCandidate>()
            .ForMember(dst => dst.CandidateReferenceId, opts => opts.MapFrom(src => src.CandidateId));
        CreateMap<MajorityElectionCandidate, SecondaryMajorityElectionCandidate>()
            .ForMember(dst => dst.BallotGroupEntries, opts => opts.Ignore());
        CreateMap<MajorityElectionCandidateTranslation, SecondaryMajorityElectionCandidateTranslation>()
            .ForMember(dst => dst.Id, opts => opts.Ignore());

        CreateMap<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated, SecondaryMajorityElectionCandidate>(MemberList.Source)
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<SecondaryMajorityElectionCandidateTranslation>(
                ((t, x) => t.Occupation = x, src.Occupation),
                ((t, x) => t.OccupationTitle = x, src.OccupationTitle),
                ((t, x) => t.Party = x, src.Party))))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate());

        CreateMap<MajorityElectionBallotGroupEntryEventData, MajorityElectionBallotGroupEntry>()
            .ForMember(dst => dst.SecondaryMajorityElectionId, opts => opts.MapFrom(src => src.ElectionId));
        CreateMap<MajorityElectionBallotGroupEventData, MajorityElectionBallotGroup>()
            .AfterMap((_, ballotGroup) =>
            {
                var primaryElectionId = ballotGroup.MajorityElectionId;
                var primaryElectionEntry = ballotGroup.Entries.FirstOrDefault(e => e.SecondaryMajorityElectionId == primaryElectionId);
                if (primaryElectionEntry != null)
                {
                    primaryElectionEntry.PrimaryMajorityElectionId = primaryElectionId;
                    primaryElectionEntry.SecondaryMajorityElectionId = null;
                }
            });
    }
}
