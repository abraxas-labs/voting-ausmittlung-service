// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.Data.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

// this test ensures these id's stay consistent
public class AusmittlungUuidV5Test
{
    [Fact]
    public void TestBuildContestCountingCircleDetailsTestingPhase()
    {
        AusmittlungUuidV5.BuildContestCountingCircleDetails(
                Guid.Parse("910ac41d-e477-4f87-8df9-750372e01902"), Guid.Parse("e109de03-43bf-44a6-bc85-e127257700f9"), false)
            .Should()
            .Be(Guid.Parse("dd92a48e-2f0d-52c9-a327-b93b1c48c9f2"));
    }

    [Fact]
    public void TestBuildContestCountingCircleDetailsTestingPhaseEnded()
    {
        AusmittlungUuidV5.BuildContestCountingCircleDetails(
                Guid.Parse("910ac41d-e477-4f87-8df9-750372e01902"), Guid.Parse("e109de03-43bf-44a6-bc85-e127257700f9"), true)
            .Should()
            .Be(Guid.Parse("694575a6-f4fe-5a09-a1fd-ad4b353c3c0f"));
    }

    [Fact]
    public void TestBuildContestImportsTestingPhase()
    {
        AusmittlungUuidV5.BuildContestImports(
                Guid.Parse("b2128787-27fb-4861-89b9-58af47dfd2e2"), false)
            .Should()
            .Be(Guid.Parse("84368243-5fd7-58c1-a2be-32a6b5604928"));
    }

    [Fact]
    public void BuildContestImports()
    {
        AusmittlungUuidV5.BuildContestImports(
                Guid.Parse("b2128787-27fb-4861-89b9-58af47dfd2e2"), true)
            .Should()
            .Be(Guid.Parse("e1bdbf8c-49e9-59f4-9f61-edabaff80dad"));
    }

    [Fact]
    public void TestBuildContestCountingCircleImportsTestingPhase()
    {
        AusmittlungUuidV5.BuildContestCountingCircleImports(
                Guid.Parse("b2128787-27fb-4861-89b9-58af47dfd2e2"), Guid.Parse("db5f1dd5-69d2-4f45-9d42-a63f2ca2a953"), false)
            .Should()
            .Be(Guid.Parse("099847c2-076c-5d35-9a72-02440724e07a"));
    }

    [Fact]
    public void TestBuildContestCountingCircleImportsTestingPhaseEnded()
    {
        AusmittlungUuidV5.BuildContestCountingCircleImports(
                Guid.Parse("b2128787-27fb-4861-89b9-58af47dfd2e2"), Guid.Parse("db5f1dd5-69d2-4f45-9d42-a63f2ca2a953"), true)
            .Should()
            .Be(Guid.Parse("843b1bf2-0d3e-5abb-91f1-d6d0bd3dc4e3"));
    }

    [Fact]
    public void TestBuildVoteBallotResult()
    {
        AusmittlungUuidV5.BuildVoteBallotResult(
                Guid.Parse("3ce7a72f-fee4-4743-aed6-55b99c23b9bc"), Guid.Parse("8b08400d-6ecb-461d-94d4-0369d47a014e"))
            .Should()
            .Be(Guid.Parse("f0e56861-577e-5c0d-b871-1cb73526da8f"));
    }

    [Fact]
    public void TestBuildResultExportConfiguration()
    {
        AusmittlungUuidV5.BuildResultExportConfiguration(
                Guid.Parse("f2461d6f-f975-4d71-a579-e11efdfab1ab"), Guid.Parse("b37a9240-9918-4fd1-bcd1-2483bf330ec1"))
            .Should()
            .Be(Guid.Parse("eaae00ff-6069-5436-a83e-20a852e95d16"));
    }

    [Fact]
    public void TestBuildDomainOfInfluenceParty()
    {
        AusmittlungUuidV5.BuildDomainOfInfluenceParty(
                Guid.Parse("7bda65a0-e856-4456-8004-5446e5e9964a"), Guid.Parse("f9258b80-2856-4de0-83e9-0ecb65571b53"))
            .Should()
            .Be(Guid.Parse("26b545a8-d84a-5ecf-b6be-58eb1fe4eed8"));
    }

    [Fact]
    public void TestBuildDomainOfInfluenceSnapshot()
    {
        AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(
                Guid.Parse("bfb7360b-ac4f-46e2-b089-594e7c699a12"), Guid.Parse("66c8e789-27c5-47a5-937d-acbedc199e75"))
            .Should()
            .Be(Guid.Parse("a195eb0e-ba84-5434-ae44-4ce2f09a7658"));
    }

    [Fact]
    public void TestBuildCountingCircleSnapshot()
    {
        AusmittlungUuidV5.BuildCountingCircleSnapshot(
                Guid.Parse("78db3670-21ff-4ff7-8e5f-5f9a2933e99f"), Guid.Parse("4807ac9a-ccf3-4e40-807d-8a11d16409af"))
            .Should()
            .Be(Guid.Parse("8c300efd-2415-565b-a86e-b531d21d11a5"));
    }

    [Fact]
    public void TestBuildPoliticalBusinessResultTestingPhase()
    {
        AusmittlungUuidV5.BuildPoliticalBusinessResult(
                Guid.Parse("947e01d9-0a56-4ab1-95cf-320a7af5b4bc"), Guid.Parse("4807ac9a-ccf3-4e40-807d-8a11d16409af"), false)
            .Should()
            .Be(Guid.Parse("d6de3191-4ea7-5eff-bfc4-284724818888"));
    }

    [Fact]
    public void TestBuildPoliticalBusinessResultTestingPhaseEnded()
    {
        AusmittlungUuidV5.BuildPoliticalBusinessResult(
                Guid.Parse("947e01d9-0a56-4ab1-95cf-320a7af5b4bc"), Guid.Parse("4807ac9a-ccf3-4e40-807d-8a11d16409af"), true)
            .Should()
            .Be(Guid.Parse("01f6dafd-67d5-5ba3-8ea1-b68c8c9b8188"));
    }

    [Fact]
    public void TestBuildPoliticalBusinessEndResultTestingPhase()
    {
        AusmittlungUuidV5.BuildPoliticalBusinessEndResult(Guid.Parse("891ec272-af6d-4ee3-8524-8b6c02e6541c"), false)
            .Should()
            .Be(Guid.Parse("b582cb95-f2b5-588b-b2e9-a42916c8e278"));
    }

    [Fact]
    public void TestBuildPoliticalBusinessEndResultTestingPhaseEnded()
    {
        AusmittlungUuidV5.BuildPoliticalBusinessEndResult(Guid.Parse("891ec272-af6d-4ee3-8524-8b6c02e6541c"), true)
            .Should()
            .Be(Guid.Parse("2fca3f3b-7cc8-5168-9801-c2fb8a0244d1"));
    }
}
