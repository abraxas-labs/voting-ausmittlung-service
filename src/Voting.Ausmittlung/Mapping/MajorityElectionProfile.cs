// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class MajorityElectionProfile : Profile
{
    public MajorityElectionProfile()
    {
        // read
        CreateMap<DataModels.Election, ProtoModels.SimplePoliticalBusiness>();
        CreateMap<DataModels.MajorityElection, ProtoModels.MajorityElection>()
            .ForMember(dst => dst.DomainOfInfluenceId, opts => opts.MapFrom(src => src.DomainOfInfluence.BasisDomainOfInfluenceId))
            .ForMember(dst => dst.CountOfSecondaryElections, opts => opts.MapFrom(src => src.ElectionGroup == null ? 0 : src.ElectionGroup.CountOfSecondaryElections));
        CreateMap<DataModels.SecondaryMajorityElection, ProtoModels.SecondaryMajorityElection>();
        CreateMap<DataModels.MajorityElection, ProtoModels.MajorityElectionCandidates>()
            .ForMember(dst => dst.Candidates, opts => opts.MapFrom(src => src.MajorityElectionCandidates))
            .ForMember(dst => dst.SecondaryElectionCandidates, opts => opts.MapFrom(src => src.SecondaryMajorityElections));
        CreateMap<DataModels.SecondaryMajorityElection, ProtoModels.SecondaryMajorityElectionCandidates>()
            .ForMember(dst => dst.SecondaryMajorityElectionId, opts => opts.MapFrom(src => src.Id));
        CreateMap<DataModels.MajorityElectionCandidateBase, ProtoModels.MajorityElectionCandidate>();
        CreateMap<DataModels.MajorityElectionBallotGroup, ProtoModels.MajorityElectionBallotGroup>();
        CreateMap<DataModels.MajorityElectionBallotGroupEntry, ProtoModels.MajorityElectionBallotGroupEntry>()
            .ForMember(dst => dst.Election, opts =>
            {
                opts.Condition(x => x.PrimaryMajorityElection != null);
                opts.MapFrom(src => src.PrimaryMajorityElection);
            })
            .ForMember(dst => dst.SecondaryElection, opts =>
            {
                opts.Condition(x => x.SecondaryMajorityElection != null);
                opts.MapFrom(src => src.SecondaryMajorityElection);
            });
        CreateMap<DataModels.MajorityElectionBallotGroupEntryCandidate, ProtoModels.MajorityElectionCandidate>()
            .IncludeMembers(src => src.Candidate);
    }
}
