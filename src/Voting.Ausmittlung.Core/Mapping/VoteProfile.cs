// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class VoteProfile : Profile
{
    public VoteProfile()
    {
        CreateMap<VoteEventData, Vote>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<VoteTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.OfficialDescription = x, src.OfficialDescription))));
        CreateMap<VoteAfterTestingPhaseUpdated, Vote>(MemberList.Source)
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate())
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<VoteTranslation>(
                ((t, x) => t.ShortDescription = x, src.ShortDescription),
                ((t, x) => t.OfficialDescription = x, src.OfficialDescription))));

        CreateMap<Vote, SimplePoliticalBusiness>()
            .ForMember(dst => dst.PoliticalBusinessTranslations, opts => opts.Ignore())
            .ForMember(dst => dst.CountingCircleResults, opts => opts.Ignore())
            .ForMember(dst => dst.SwissAbroadVotingRight, opts => opts.Ignore())
            .AfterMap((_, dst) => dst.PoliticalBusinessType = PoliticalBusinessType.Vote);

        CreateMap<VoteTranslation, SimplePoliticalBusinessTranslation>()
            .ForMember(dst => dst.Id, opts => opts.Ignore());

        CreateMap<BallotEventData, Ballot>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<BallotTranslation>(((t, x) => t.Description = x, src.Description))))
            .AfterMap((_, dst) =>
            {
                foreach (var q in dst.BallotQuestions)
                {
                    q.BallotId = dst.Id;
                }

                foreach (var q in dst.TieBreakQuestions)
                {
                    q.BallotId = dst.Id;
                }
            });
        CreateMap<BallotAfterTestingPhaseUpdated, Ballot>(MemberList.Source)
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<BallotTranslation>(((t, x) => t.Description = x, src.Description))))
            .ForSourceMember(src => src.EventInfo, opts => opts.DoNotValidate())
            .AfterMap((_, dst) =>
            {
                foreach (var q in dst.BallotQuestions)
                {
                    q.BallotId = dst.Id;
                }

                foreach (var q in dst.TieBreakQuestions)
                {
                    q.BallotId = dst.Id;
                }
            });

        CreateMap<BallotQuestionEventData, BallotQuestion>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<BallotQuestionTranslation>(
                ((t, x) => t.Question = x, src.Question))));
        CreateMap<TieBreakQuestionEventData, TieBreakQuestion>()
            .ForMember(dst => dst.Translations, opts => opts.MapFrom((src, _) => TranslationBuilder.CreateTranslations<TieBreakQuestionTranslation>(
                ((t, x) => t.Question = x, src.Question))));
    }
}
