// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using FluentAssertions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Lib.Testing;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.EnumTests;

public class EnumTest : BaseTest<TestApplicationFactory, TestStartup>
{
    private readonly TestMapper _mapper;

    public EnumTest(TestApplicationFactory factory)
        : base(factory)
    {
        _mapper = GetService<TestMapper>();
    }

    [Theory]
    [InlineData(typeof(SexType), typeof(SharedProto.SexType))]
    [InlineData(typeof(MajorityElectionResultEntry), typeof(SharedProto.MajorityElectionResultEntry))]
    [InlineData(typeof(BallotBundleState), typeof(ProtoModels.BallotBundleState))]
    [InlineData(typeof(ContestState), typeof(ProtoModels.ContestState))]
    [InlineData(typeof(CountingCircleResultState), typeof(ProtoModels.CountingCircleResultState))]
    [InlineData(typeof(DomainOfInfluenceCanton), typeof(ProtoModels.DomainOfInfluenceCanton))]
    [InlineData(typeof(Lib.VotingExports.Models.ExportFileFormat), typeof(ProtoModels.ExportFileFormat))]
    [InlineData(typeof(Lib.VotingExports.Models.EntityType), typeof(ProtoModels.ExportEntityType))]
    [InlineData(typeof(VotingChannel), typeof(SharedProto.VotingChannel))]
    [InlineData(typeof(MajorityElectionMandateAlgorithm), typeof(ProtoModels.MajorityElectionMandateAlgorithm))]
    [InlineData(typeof(MajorityElectionCandidateEndResultState), typeof(ProtoModels.MajorityElectionCandidateEndResultState))]
    [InlineData(typeof(PoliticalBusinessType), typeof(ProtoModels.PoliticalBusinessType))]
    [InlineData(typeof(PoliticalBusinessUnionType), typeof(ProtoModels.PoliticalBusinessUnionType))]
    [InlineData(typeof(ProportionalElectionMandateAlgorithm), typeof(ProtoModels.ProportionalElectionMandateAlgorithm))]
    [InlineData(typeof(ProportionalElectionCandidateEndResultState), typeof(ProtoModels.ProportionalElectionCandidateEndResultState))]
    [InlineData(typeof(SwissAbroadVotingRight), typeof(ProtoModels.SwissAbroadVotingRight))]
    [InlineData(typeof(BallotType), typeof(ProtoModels.BallotType))]
    [InlineData(typeof(VoteResultAlgorithm), typeof(ProtoModels.VoteResultAlgorithm))]
    [InlineData(typeof(BallotNumberGeneration), typeof(SharedProto.BallotNumberGeneration))]
    [InlineData(typeof(DomainOfInfluenceType), typeof(SharedProto.DomainOfInfluenceType))]
    [InlineData(typeof(MajorityElectionWriteInMappingTarget), typeof(SharedProto.MajorityElectionWriteInMappingTarget))]
    [InlineData(typeof(VoteResultEntry), typeof(SharedProto.VoteResultEntry))]
    [InlineData(typeof(BallotQuestionAnswer), typeof(SharedProto.BallotQuestionAnswer))]
    [InlineData(typeof(TieBreakQuestionAnswer), typeof(SharedProto.TieBreakQuestionAnswer))]
    [InlineData(typeof(VoterType), typeof(SharedProto.VoterType))]
    public void ShouldBeSameEnum(Type dataEnumType, Type protoEnumType)
    {
        CompareEnums(dataEnumType, protoEnumType);
        MappingTest(dataEnumType, protoEnumType);
    }

    [Fact]
    public void ShouldThrowAutoMapperException()
    {
        Assert.Throws<AutoMapperMappingException>(() => _mapper.Map<EnumMockedData.TestEnum1>(EnumMockedData.TestEnum2.ValueC));
        Assert.Throws<AutoMapperMappingException>(() => _mapper.Map<EnumMockedData.TestEnum1>(EnumMockedData.TestEnum2.ValueB2));
    }

    private static void CompareEnums(Type dataEnumType, Type protoEnumType)
    {
        var dataEnumArray = (int[])Enum.GetValues(dataEnumType);
        var protoEnumArray = (int[])Enum.GetValues(protoEnumType);

        dataEnumArray.Length.Should().Be(protoEnumArray.Length);

        foreach (var value in dataEnumArray)
        {
            var dataEnumName = Enum.GetName(dataEnumType, value);
            var protoEnumName = Enum.GetName(protoEnumType, value);
            dataEnumName.Should().Be(protoEnumName);
        }

        for (var i = 0; i < protoEnumArray.Length; i++)
        {
            dataEnumArray[i].Should().Be(protoEnumArray[i]);
        }
    }

    private void MappingTest(Type dataEnumType, Type protoEnumType)
    {
        var dataEnumArray = (int[])Enum.GetValues(dataEnumType);
        var protoEnumArray = (int[])Enum.GetValues(protoEnumType);

        for (var i = 0; i < protoEnumArray.Length; i++)
        {
            var dataEnum = Enum.ToObject(dataEnumType, dataEnumArray[i]);
            var protoEnum = Enum.ToObject(protoEnumType, protoEnumArray[i]);

            var mappedProtoValue = _mapper.Map(dataEnum, dataEnumType, protoEnumType);
            mappedProtoValue.Should().Be(protoEnum);

            var mappedDataValue = _mapper.Map(protoEnum, protoEnumType, dataEnumType);
            mappedDataValue.Should().Be(dataEnum);
        }
    }
}
