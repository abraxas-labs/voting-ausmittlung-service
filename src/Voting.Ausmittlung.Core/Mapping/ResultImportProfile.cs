// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class ResultImportProfile : Profile
{
    public ResultImportProfile()
    {
        CreateMap<MajorityElectionResultImport, MajorityElectionResultImported>()
            .ForMember(dst => dst.MajorityElectionId, opts => opts.MapFrom(x => x.PoliticalBusinessId))
            .ForMember(x => x.CandidateResults, opts => opts.MapFrom(x => x.CandidateVoteCounts))
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(x => x.BasisCountingCircleId))
            .ForMember(dst => dst.WriteIns, opts => opts.MapFrom(src => src.WriteIns));
        CreateMap<MajorityElectionResultImport, SecondaryMajorityElectionResultImported>()
            .ForMember(dst => dst.SecondaryMajorityElectionId, opts => opts.MapFrom(x => x.PoliticalBusinessId))
            .ForMember(x => x.CandidateResults, opts => opts.MapFrom(x => x.CandidateVoteCounts))
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(x => x.BasisCountingCircleId))
            .ForMember(dst => dst.WriteIns, opts => opts.MapFrom(src => src.WriteIns));
        CreateMap<KeyValuePair<Guid, int>, MajorityElectionCandidateResultImportEventData>()
            .ConvertUsing(kvp => new MajorityElectionCandidateResultImportEventData
            {
                CandidateId = kvp.Key.ToString(),
                VoteCount = kvp.Value,
            });
        CreateMap<KeyValuePair<string, WriteInMapping>, MajorityElectionWriteInEventData>()
            .ConvertUsing(kvp => new MajorityElectionWriteInEventData
            {
                WriteInMappingId = kvp.Value.Id.ToString(),
                WriteInCandidateName = kvp.Key,
                VoteCount = kvp.Value.CountOfVotes,
            });

        CreateMap<ProportionalElectionResultImport, ProportionalElectionResultImported>()
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(x => x.BasisCountingCircleId));
        CreateMap<ProportionalElectionListResultImport, ProportionalElectionListResultImportEventData>();
        CreateMap<ProportionalElectionCandidateResultImport, ProportionalElectionCandidateResultImportEventData>();
        CreateMap<KeyValuePair<Guid, int>, ProportionalElectionCandidateVoteSourceResultImportEventData>()
            .ConvertUsing(kvp => new ProportionalElectionCandidateVoteSourceResultImportEventData
            {
                ListId = kvp.Key == Guid.Empty ? string.Empty : kvp.Key.ToString(),
                VoteCount = kvp.Value,
            });

        CreateMap<VoteResultImport, VoteResultImported>()
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(x => x.BasisCountingCircleId));
        CreateMap<VoteBallotResultImport, VoteBallotResultImportEventData>();
        CreateMap<BallotQuestionResultImport, BallotQuestionResultImportEventData>();
        CreateMap<TieBreakQuestionResultImport, TieBreakQuestionResultImportEventData>();

        CreateMap<ImportIgnoredCountingCircleEventData, IgnoredImportCountingCircle>()
            .ReverseMap();

        CreateMap<CountingCircleResultCountOfVotersInformationImport, CountingCircleResultCountOfVotersInformationImportEventData>();
    }
}
