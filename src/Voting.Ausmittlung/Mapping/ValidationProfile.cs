// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using DataModels = Voting.Ausmittlung.Core.Services.Validation.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ValidationProfile : Profile
{
    public ValidationProfile()
    {
        // read
        CreateMap<DataModels.ValidationResult, ProtoModels.ValidationResult>()
            .AfterMap((src, dst, ctx) =>
            {
                switch (src.Data)
                {
                    case DataModels.ValidationPoliticalBusinessData x:
                        dst.PoliticalBusinessData = ctx.Mapper.Map<ProtoModels.ValidationPoliticalBusinessData>(x);
                        break;

                    case DataModels.ValidationVoteAccountedBallotsEqualQnData x:
                        dst.VoteAccountedBallotsEqualQnData = ctx.Mapper.Map<ProtoModels.ValidationVoteAccountedBallotsEqualQnData>(x);
                        break;

                    case DataModels.ValidationMajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotesData x:
                        dst.MajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyInvalidVotesData = ctx.Mapper.Map<ProtoModels.ValidationMajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotesData>(x);
                        break;

                    case DataModels.ValidationProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedListsData x:
                        dst.ProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedListsData = ctx.Mapper.Map<ProtoModels.ValidationProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedListsData>(x);
                        break;

                    case DataModels.ValidationComparisonVoterParticipationsData x:
                        dst.ComparisonVoterParticipationsData = ctx.Mapper.Map<ProtoModels.ValidationComparisonVoterParticipationsData>(x);
                        break;

                    case DataModels.ValidationComparisonCountOfVotersData x:
                        dst.ComparisonCountOfVotersData = ctx.Mapper.Map<ProtoModels.ValidationComparisonCountOfVotersData>(x);
                        break;

                    case DataModels.ValidationComparisonVotingChannelsData x:
                        dst.ComparisonVotingChannelsData = ctx.Mapper.Map<ProtoModels.ValidationComparisonVotingChannelsData>(x);
                        break;

                    case DataModels.ValidationComparisonValidVotingCardsWithAccountedBallotsData x:
                        dst.ComparisonValidVotingCardsWithAccountedBallotsData = ctx.Mapper.Map<ProtoModels.ValidationComparisonValidVotingCardsWithAccountedBallotsData>(x);
                        break;

                    case DataModels.ValidationProportionalElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRowsData x:
                        dst.ProportionalElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRowsData = ctx.Mapper.Map<ProtoModels.ValidationProportionalElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRowsData>(x);
                        break;
                }
            });
        CreateMap<DataModels.ValidationPoliticalBusinessData, ProtoModels.ValidationPoliticalBusinessData>();
        CreateMap<DataModels.ValidationVoteAccountedBallotsEqualQnData, ProtoModels.ValidationVoteAccountedBallotsEqualQnData>();
        CreateMap<DataModels.ValidationMajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotesData, ProtoModels.ValidationMajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotesData>();
        CreateMap<DataModels.ValidationProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedListsData, ProtoModels.ValidationProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedListsData>();
        CreateMap<DataModels.ValidationComparisonVoterParticipationsData, ProtoModels.ValidationComparisonVoterParticipationsData>();
        CreateMap<DataModels.ValidationComparisonCountOfVotersData, ProtoModels.ValidationComparisonCountOfVotersData>();
        CreateMap<DataModels.ValidationComparisonVotingChannelsData, ProtoModels.ValidationComparisonVotingChannelsData>();
        CreateMap<DataModels.ValidationComparisonValidVotingCardsWithAccountedBallotsData, ProtoModels.ValidationComparisonValidVotingCardsWithAccountedBallotsData>();
        CreateMap<DataModels.ValidationProportionalElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRowsData, ProtoModels.ValidationProportionalElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRowsData>();

        CreateMap<DataModels.ValidationSummary, ProtoModels.ValidationSummary>();
        CreateMap<List<DataModels.ValidationSummary>, ProtoModels.ValidationSummaries>()
            .ForMember(dst => dst.Summaries, opts => opts.MapFrom(src => src))
            .ForMember(dst => dst.IsValid, opts => opts.MapFrom(src => src.All(x => x.ValidationResults.IsValid())));
    }
}
