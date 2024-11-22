// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using FluentAssertions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Lib.Testing;
using Xunit;
using BasisSharedProto = Abraxas.Voting.Basis.Shared.V1;
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
    [InlineData(typeof(ProportionalElectionCandidateEndResultState), typeof(SharedProto.ProportionalElectionCandidateEndResultState))]
    [InlineData(typeof(BallotType), typeof(ProtoModels.BallotType))]
    [InlineData(typeof(VoteResultAlgorithm), typeof(ProtoModels.VoteResultAlgorithm))]
    [InlineData(typeof(BallotNumberGeneration), typeof(SharedProto.BallotNumberGeneration))]
    [InlineData(typeof(DomainOfInfluenceType), typeof(SharedProto.DomainOfInfluenceType))]
    [InlineData(typeof(MajorityElectionWriteInMappingTarget), typeof(SharedProto.MajorityElectionWriteInMappingTarget))]
    [InlineData(typeof(VoteResultEntry), typeof(SharedProto.VoteResultEntry))]
    [InlineData(typeof(BallotQuestionAnswer), typeof(SharedProto.BallotQuestionAnswer))]
    [InlineData(typeof(TieBreakQuestionAnswer), typeof(SharedProto.TieBreakQuestionAnswer))]
    [InlineData(typeof(VoterType), typeof(SharedProto.VoterType))]
    [InlineData(typeof(BallotQuestionType), typeof(ProtoModels.BallotQuestionType))]
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

    [Theory]
    [InlineData(typeof(ProtoModels.ProportionalElectionMandateAlgorithm))]
    [InlineData(typeof(BasisSharedProto.ProportionalElectionMandateAlgorithm))]
    public void ShouldMapProportionalElectionMandateAlgorithm(Type protoEnumType)
    {
        var dataEnumType = typeof(ProportionalElectionMandateAlgorithm);
        var dataEnumArray = (int[])Enum.GetValues(dataEnumType);
        var protoEnumArray = (int[])Enum.GetValues(protoEnumType);

        // 2 deprecated proto enum values which aren't used in data anymore.
        dataEnumArray.Length.Should().Be(protoEnumArray.Length - 2);

        // data enum is a subset of the proto enum.
        foreach (var value in dataEnumArray)
        {
            var dataEnumName = Enum.GetName(dataEnumType, value);
            var protoEnumName = Enum.GetName(protoEnumType, value);
            dataEnumName.Should().Be(protoEnumName);
        }

        var expectedProtoEnumMappingResult = new[]
        {
            ProportionalElectionMandateAlgorithm.Unspecified,
            ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum, // for deprecated DoppelterPukelsheim5Quorum
            ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum, // for deprecated DoppelterPukelsheim0Quorum
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum,
            ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum,
        };

        for (var i = 0; i < protoEnumArray.Length; i++)
        {
            var dataEnum = Enum.ToObject(dataEnumType, expectedProtoEnumMappingResult[i]);
            var protoEnum = Enum.ToObject(protoEnumType, protoEnumArray[i]);

            var mappedDataValue = _mapper.Map(protoEnum, protoEnumType, dataEnumType);
            mappedDataValue.Should().Be(dataEnum);
        }

        var deprecatedCounter = 0; // counter to skip in the proto enum the deprecated values
        for (var i = 0; i < dataEnumArray.Length; i++)
        {
            if (i == 2)
            {
                deprecatedCounter += 2;
            }

            var dataEnum = Enum.ToObject(dataEnumType, dataEnumArray[i]);
            var protoEnum = Enum.ToObject(protoEnumType, protoEnumArray[i + deprecatedCounter]);

            var mappedProtoValue = _mapper.Map(dataEnum, dataEnumType, protoEnumType);
            mappedProtoValue.Should().Be(protoEnum);
        }
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
