// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using AutoMapper.Extensions.EnumMapping;
using Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Mapping;

/// <summary>
/// An entry for every enum is needed since AutoMapper doesnt have a property to enable mapByName globally.
/// If an entry for an enum is not defined AutoMapper will map the enums by their values.
/// </summary>
public class EnumProfile : Profile
{
    public EnumProfile()
    {
        CreateEnumMap<SharedProto.SexType, SexType>();
        CreateEnumMap<SharedProto.MajorityElectionResultEntry, MajorityElectionResultEntry>();
        CreateEnumMap<ProtoModels.BallotBundleState, BallotBundleState>();
        CreateEnumMap<ProtoModels.ContestState, ContestState>();
        CreateEnumMap<ProtoModels.CountingCircleResultState, CountingCircleResultState>();
        CreateEnumMap<ProtoModels.DomainOfInfluenceCanton, DomainOfInfluenceCanton>();
        CreateEnumMap<ProtoModels.ExportFileFormat, Lib.VotingExports.Models.ExportFileFormat>();
        CreateEnumMap<ProtoModels.ExportEntityType, Lib.VotingExports.Models.EntityType>();
        CreateEnumMap<ProtoModels.ExportProvider, ExportProvider>();
        CreateEnumMap<SharedProto.VotingChannel, VotingChannel>();
        CreateEnumMap<ProtoModels.MajorityElectionMandateAlgorithm, MajorityElectionMandateAlgorithm>();
        CreateEnumMap<ProtoModels.MajorityElectionCandidateEndResultState, MajorityElectionCandidateEndResultState>();
        CreateEnumMap<ProtoModels.PoliticalBusinessType, PoliticalBusinessType>();
        CreateEnumMap<ProtoModels.PoliticalBusinessUnionType, PoliticalBusinessUnionType>();
        CreateEnumMap<ProtoModels.ProportionalElectionMandateAlgorithm, ProportionalElectionMandateAlgorithm>();
        CreateEnumMap<SharedProto.ProportionalElectionCandidateEndResultState, ProportionalElectionCandidateEndResultState>();
        CreateEnumMap<ProtoModels.SwissAbroadVotingRight, SwissAbroadVotingRight>();
        CreateEnumMap<ProtoModels.BallotType, BallotType>();
        CreateEnumMap<ProtoModels.VoteResultAlgorithm, VoteResultAlgorithm>();
        CreateEnumMap<SharedProto.BallotNumberGeneration, BallotNumberGeneration>();
        CreateEnumMap<SharedProto.DomainOfInfluenceType, DomainOfInfluenceType>();
        CreateEnumMap<SharedProto.MajorityElectionWriteInMappingTarget, MajorityElectionWriteInMappingTarget>();
        CreateEnumMap<SharedProto.VoteResultEntry, VoteResultEntry>();
        CreateEnumMap<SharedProto.BallotQuestionAnswer, BallotQuestionAnswer>();
        CreateEnumMap<SharedProto.TieBreakQuestionAnswer, TieBreakQuestionAnswer>();
        CreateEnumMap<SharedProto.VoterType, VoterType>();
        CreateEnumMap<ProtoModels.BallotQuestionType, BallotQuestionType>();

        // explicitly map deprecated values to the corresponding new value.
        CreateMap<ProtoModels.ProportionalElectionMandateAlgorithm, ProportionalElectionMandateAlgorithm>()
            .ConvertUsingEnumMapping(opt => opt
                .MapByName()
                .MapValue(ProtoModels.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum, ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum)
                .MapValue(ProtoModels.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum, ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum))
            .ReverseMap();
    }

    private void CreateEnumMap<T1, T2>()
        where T1 : struct, Enum
        where T2 : struct, Enum
    {
        CreateMap<T1, T2>()
            .ConvertUsingEnumMapping(opt => opt.MapByName())
            .ReverseMap();
    }
}
