// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Services.V1.Responses;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ProportionalElectionResultProfile : Profile
{
    public ProportionalElectionResultProfile()
    {
        // read
        CreateMap<DataModels.ProportionalElectionResult, ProtoModels.ProportionalElectionResult>()
            .ForMember(dst => dst.Election, opts => opts.MapFrom(src => src.ProportionalElection))
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(src => src.CountingCircle.BasisCountingCircleId));
        CreateMap<DataModels.ProportionalElectionResultSubTotal, ProtoModels.ProportionalElectionResultSubTotal>();
        CreateMap<DataModels.ProportionalElectionResultEntryParams, ProtoModels.ProportionalElectionResultEntryParams>();
        CreateMap<DataModels.ProportionalElectionResult, ProtoModels.ProportionalElectionUnmodifiedListResults>()
            .ForMember(dst => dst.ElectionResult, opts => opts.MapFrom(src => src));
        CreateMap<DataModels.ProportionalElectionUnmodifiedListResult, ProtoModels.ProportionalElectionUnmodifiedListResult>();

        CreateMap<IEnumerable<DataModels.ProportionalElectionListResult>, ProtoModels.ProportionalElectionListResults>()
            .ForMember(dst => dst.ListResults, opts => opts.MapFrom(src => src));
        CreateMap<DataModels.ProportionalElectionListResult, ProtoModels.ProportionalElectionListResult>();
        CreateMap<DataModels.ProportionalElectionListResultSubTotal, ProtoModels.ProportionalElectionListResultSubTotal>();
        CreateMap<DataModels.ProportionalElectionCandidateResult, ProtoModels.ProportionalElectionCandidateResult>();
        CreateMap<DataModels.ProportionalElectionCandidateResultSubTotal, ProtoModels.ProportionalElectionCandidateResultSubTotal>();

        CreateMap<DataModels.ProportionalElectionResult, ProtoModels.ProportionalElectionResultBundles>()
            .ForMember(dst => dst.ElectionResult, opts => opts.MapFrom(src => src));
        CreateMap<IEnumerable<DataModels.ProportionalElectionResultBundle>, ProtoModels.ProportionalElectionResultBundles>()
            .ForMember(dst => dst.Bundles, opts => opts.MapFrom(src => src));
        CreateMap<DataModels.ProportionalElectionResultBundle, ProtoModels.ProportionalElectionResultBundle>();

        CreateMap<DataModels.ProportionalElectionResultBundle, GetProportionalElectionResultBundleResponse>()
            .ForMember(dst => dst.Bundle, opts => opts.MapFrom(src => src));

        CreateMap<DataModels.ProportionalElectionResultBallot, ProtoModels.ProportionalElectionResultBallot>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(c => c.BallotCandidates));

        CreateMap<DataModels.ProportionalElectionCandidate, ProtoModels.ProportionalElectionBallotCandidate>();
        CreateMap<DataModels.ProportionalElectionResultBallotCandidate, ProtoModels.ProportionalElectionBallotCandidate>()
            .IncludeMembers(x => x.Candidate)
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.CandidateId))
            .ForMember(dst => dst.ListId, opts => opts.MapFrom(src => src.Candidate.ProportionalElectionListId))
            .ForMember(dst => dst.ListPosition, opts => opts.MapFrom(src => src.Candidate.ProportionalElectionList.Position))
            .ForMember(dst => dst.ListNumber, opts => opts.MapFrom(src => src.Candidate.ProportionalElectionList.OrderNumber))
            .ForMember(dst => dst.ListDescription, opts => opts.MapFrom(src => src.Candidate.ProportionalElectionList.Description))
            .ForMember(dst => dst.ListShortDescription, opts => opts.MapFrom(src => src.Candidate.ProportionalElectionList.ShortDescription));

        // write
        CreateMap<DefineProportionalElectionResultEntryParamsRequest, ProportionalElectionResultEntryParams>();
        CreateMap<CreateUpdateProportionalElectionResultBallotCandidateRequest, ProportionalElectionResultBallotCandidate>();
        CreateMap<EnterProportionalElectionUnmodifiedListResultRequest, ProportionalElectionUnmodifiedListResult>();
    }
}
