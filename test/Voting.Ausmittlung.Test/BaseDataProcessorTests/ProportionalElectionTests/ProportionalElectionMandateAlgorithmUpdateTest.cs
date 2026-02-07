// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionMandateAlgorithmUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionMandateAlgorithmUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionEndResultMockedData.Seed(RunScoped, DataModels.ProportionalElectionMandateAlgorithm.HagenbachBischoff, 5);
    }

    [Fact]
    public async Task TestMandateAlgorithmUpdate()
    {
        await SeedHagenbachBischoffResults();

        await TestEventPublisher.Publish(
            new ProportionalElectionMandateAlgorithmUpdated
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            });

        var endResult = await RunOnDb(db =>
            db.ProportionalElectionEndResult
                .AsSplitQuery()
                .Include(x => x.HagenbachBischoffRootGroup)
                .Include(x => x.ProportionalElection)
                .Include(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                .FirstOrDefaultAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId)));
        endResult!.ProportionalElection.MandateAlgorithm.Should().Be(DataModels.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum);

        endResult.HagenbachBischoffRootGroup.Should().BeNull();
        EnsureEndResultsAreResetted(endResult);

        await SeedDoubleProportionalResults();
        await TestEventPublisher.Publish(
            1,
            new ProportionalElectionMandateAlgorithmUpdated
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
            });

        endResult = await RunOnDb(db =>
            db.ProportionalElectionEndResult
                .AsSplitQuery()
                .Include(x => x.ProportionalElection.DoubleProportionalResult)
                .Include(x => x.ProportionalElection)
                .Include(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                .FirstOrDefaultAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId)));
        endResult!.ProportionalElection.MandateAlgorithm.Should().Be(DataModels.ProportionalElectionMandateAlgorithm.HagenbachBischoff);

        endResult.ProportionalElection.DoubleProportionalResult.Should().BeNull();
        EnsureEndResultsAreResetted(endResult);
    }

    private async Task SeedHagenbachBischoffResults()
    {
        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .FirstAsync(x =>
                    x.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId));

            endResult.HagenbachBischoffRootGroup = new DataModels.HagenbachBischoffGroup();
            endResult.ManualEndResultRequired = true;
            endResult.Finalized = true;

            var listEndResult = await db.ProportionalElectionListEndResult
                .AsTracking()
                .FirstAsync(x => x.ListId == Guid.Parse(ProportionalElectionEndResultMockedData.ListId1));

            listEndResult.NumberOfMandates = 1;
            listEndResult.LotDecisionState = DataModels.ElectionLotDecisionState.OpenAndRequired;

            var candidateEndResult = await db.ProportionalElectionCandidateEndResult
                .AsTracking()
                .FirstAsync(x => x.CandidateId == Guid.Parse(ProportionalElectionEndResultMockedData.List1CandidateId1));

            candidateEndResult.Rank = 1;
            candidateEndResult.LotDecision = true;
            candidateEndResult.LotDecisionRequired = true;
            candidateEndResult.LotDecisionEnabled = true;
            candidateEndResult.State = DataModels.ProportionalElectionCandidateEndResultState.Elected;

            await db.SaveChangesAsync();
        });
    }

    private async Task SeedDoubleProportionalResults()
    {
        await RunOnDb(async db =>
        {
            var union = new DataModels.ProportionalElectionUnion
            {
                ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                EndResult = new DataModels.ProportionalElectionUnionEndResult(),
                DoubleProportionalResult = new DataModels.DoubleProportionalResult(),
                ProportionalElectionUnionEntries = new List<DataModels.ProportionalElectionUnionEntry>
                {
                    new()
                    {
                        ProportionalElectionId = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
                    },
                },
            };
            await db.ProportionalElectionUnions.AddAsync(union);

            var election = await db.ProportionalElections
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId));

            election.DoubleProportionalResult = new DataModels.DoubleProportionalResult();

            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .FirstAsync(x =>
                    x.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId));

            endResult.ManualEndResultRequired = true;
            endResult.Finalized = true;

            var listEndResult = await db.ProportionalElectionListEndResult
                .AsTracking()
                .FirstAsync(x => x.ListId == Guid.Parse(ProportionalElectionEndResultMockedData.ListId1));

            listEndResult.NumberOfMandates = 1;
            listEndResult.LotDecisionState = DataModels.ElectionLotDecisionState.OpenAndRequired;

            var candidateEndResult = await db.ProportionalElectionCandidateEndResult
                .AsTracking()
                .FirstAsync(x => x.CandidateId == Guid.Parse(ProportionalElectionEndResultMockedData.List1CandidateId1));

            candidateEndResult.Rank = 1;
            candidateEndResult.LotDecision = true;
            candidateEndResult.LotDecisionRequired = true;
            candidateEndResult.LotDecisionEnabled = true;
            candidateEndResult.State = DataModels.ProportionalElectionCandidateEndResultState.Elected;

            await db.SaveChangesAsync();
        });
    }

    private void EnsureEndResultsAreResetted(DataModels.ProportionalElectionEndResult endResult)
    {
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.Finalized.Should().BeFalse();

        foreach (var listEndResult in endResult.ListEndResults)
        {
            listEndResult.NumberOfMandates.Should().Be(0);
            listEndResult.LotDecisionState.Should().Be(DataModels.ElectionLotDecisionState.Unspecified);

            foreach (var candidateEndResult in listEndResult.CandidateEndResults)
            {
                candidateEndResult.Rank.Should().Be(0);
                candidateEndResult.LotDecision.Should().BeFalse();
                candidateEndResult.LotDecisionRequired.Should().BeFalse();
                candidateEndResult.LotDecisionEnabled.Should().BeFalse();
                candidateEndResult.State.Should().Be(DataModels.ProportionalElectionCandidateEndResultState.Pending);
            }
        }
    }
}
