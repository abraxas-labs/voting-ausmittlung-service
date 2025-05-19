// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Services.V1.Responses;
using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using DomainModels = Voting.Ausmittlung.Core.Domain;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class MajorityElectionResultProfile : Profile
{
    public MajorityElectionResultProfile()
    {
        // read
        CreateMap<DataModels.MajorityElectionResult, ProtoModels.MajorityElectionResult>()
            .ForMember(dst => dst.Election, opts => opts.MapFrom(src => src.MajorityElection))
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(src => src.CountingCircle.BasisCountingCircleId));
        CreateMap<DataModels.SecondaryMajorityElectionResult, ProtoModels.SecondaryMajorityElectionResult>();
        CreateMap<DataModels.MajorityElectionResultEntryParams, ProtoModels.MajorityElectionResultEntryParams>();

        CreateMap<DataModels.MajorityElectionResult, ProtoModels.MajorityElectionResultBundles>()
            .ForMember(dst => dst.ElectionResult, opts => opts.MapFrom(src => src));
        CreateMap<IEnumerable<DataModels.MajorityElectionResultBundle>, ProtoModels.MajorityElectionResultBundles>()
            .ForMember(dst => dst.Bundles, opts => opts.MapFrom(src => src));
        CreateMap<DataModels.MajorityElectionResultBundle, ProtoModels.MajorityElectionResultBundle>();
        CreateMap<DataModels.MajorityElectionResultBundleLog, ProtoModels.PoliticalBusinessResultBundleLog>();

        CreateMap<DataModels.MajorityElectionResultBundle, GetMajorityElectionResultBundleResponse>()
            .ForMember(dst => dst.Bundle, opts => opts.MapFrom(src => src));

        CreateMap<DataModels.MajorityElectionCandidate, ProtoModels.MajorityElectionBallotCandidate>();
        CreateMap<DataModels.SecondaryMajorityElectionCandidate, ProtoModels.MajorityElectionBallotCandidate>();
        CreateMap<DataModels.MajorityElectionResultBallotCandidate, ProtoModels.MajorityElectionBallotCandidate>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.CandidateId))
            .IncludeMembers(x => x.Candidate);
        CreateMap<DataModels.SecondaryMajorityElectionResultBallotCandidate, ProtoModels.MajorityElectionBallotCandidate>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.CandidateId))
            .IncludeMembers(x => x.Candidate);

        CreateMap<DataModels.MajorityElectionResultBallot, ProtoModels.MajorityElectionResultBallot>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(src => src.BallotCandidates));

        CreateMap<DataModels.SecondaryMajorityElectionResultBallot, ProtoModels.SecondaryMajorityElectionResultBallot>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(src => src.BallotCandidates))
            .ForMember(dst => dst.ElectionId, opts => opts.MapFrom(src => src.SecondaryMajorityElectionResult.SecondaryMajorityElectionId));

        CreateMap<DataModels.MajorityElectionBallotGroupResult, ProtoModels.MajorityElectionBallotGroupResult>();
        CreateMap<DataModels.MajorityElectionResult, ProtoModels.MajorityElectionBallotGroupResults>()
            .ForMember(dst => dst.ElectionResult, opts => opts.MapFrom(src => src))
            .ForMember(dst => dst.BallotGroupResults, opts => opts.MapFrom(src => src.BallotGroupResults));

        CreateMap<DataModels.MajorityElectionCandidateResult, ProtoModels.MajorityElectionCandidateResult>();
        CreateMap<DataModels.SecondaryMajorityElectionCandidateResult, ProtoModels.MajorityElectionCandidateResult>();

        CreateMap<DataModels.MajorityElectionResultSubTotal, ProtoModels.MajorityElectionResultSubTotal>();
        CreateMap<DataModels.MajorityElectionResultNullableSubTotal, ProtoModels.MajorityElectionResultNullableSubTotal>();

        CreateMap<DataModels.MajorityElectionEndResultCalculation, ProtoModels.MajorityElectionEndResultCalculation>();

        // write
        CreateMap<DefineMajorityElectionResultEntryParamsRequest, DomainModels.MajorityElectionResultEntryParams>();
        CreateMap<CreateUpdateSecondaryMajorityElectionResultBallotRequest, DomainModels.SecondaryMajorityElectionResultBallot>();
        CreateMap<EnterMajorityElectionCandidateResultRequest, DomainModels.MajorityElectionCandidateResult>();
        CreateMap<EnterSecondaryMajorityElectionCandidateResultsRequest, DomainModels.SecondaryMajorityElectionCandidateResults>();
        CreateMap<EnterMajorityElectionBallotGroupResultRequest, DomainModels.MajorityElectionBallotGroupResult>();
    }
}
