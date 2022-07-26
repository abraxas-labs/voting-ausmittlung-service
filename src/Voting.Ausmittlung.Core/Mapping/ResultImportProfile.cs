// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Models.Import;

namespace Voting.Ausmittlung.Core.Mapping;

public class ResultImportProfile : Profile
{
    public ResultImportProfile()
    {
        CreateMap<MajorityElectionResultImport, MajorityElectionResultImported>()
            .ForMember(dst => dst.MajorityElectionId, opts => opts.MapFrom(x => x.PoliticalBusinessId))
            .ForMember(x => x.CandidateResults, opts => opts.MapFrom(x => x.CandidateVoteCounts))
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(x => x.BasisCountingCircleId))
            .ForMember(dst => dst.WriteIns, opts => opts.MapFrom(src => src.WriteInVoteCounts));
        CreateMap<MajorityElectionResultImport, SecondaryMajorityElectionResultImported>()
            .ForMember(dst => dst.SecondaryMajorityElectionId, opts => opts.MapFrom(x => x.PoliticalBusinessId))
            .ForMember(x => x.CandidateResults, opts => opts.MapFrom(x => x.CandidateVoteCounts))
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(x => x.BasisCountingCircleId))
            .ForMember(dst => dst.WriteIns, opts => opts.MapFrom(src => src.WriteInVoteCounts));
        CreateMap<KeyValuePair<Guid, int>, MajorityElectionCandidateResultImportEventData>()
            .ConvertUsing(kvp => new MajorityElectionCandidateResultImportEventData
            {
                CandidateId = kvp.Key.ToString(),
                VoteCount = kvp.Value,
            });
        CreateMap<KeyValuePair<string, int>, MajorityElectionWriteInEventData>()
            .ConvertUsing(kvp => new MajorityElectionWriteInEventData
            {
                WriteInMappingId = Guid.NewGuid().ToString(),
                WriteInCandidateName = kvp.Key,
                VoteCount = kvp.Value,
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
    }
}
