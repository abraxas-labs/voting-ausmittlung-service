// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionEndResultBuildTest : MajorityElectionEndResultBaseTest
{
    public MajorityElectionEndResultBuildTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestBuildAbsoluteMajorityEndResult()
    {
        await SeedElection(
            MajorityElectionResultEntry.FinalResults,
            MajorityElectionMandateAlgorithm.AbsoluteMajority,
            3,
            1);

        await StartResultSubmissions();

        var initEndResult = await GetEndResult();
        initEndResult.MatchSnapshot("init");

        var countByCandidateIds = new List<(string, int)>
            {
                (MajorityElectionEndResultMockedData.CandidateId1, 200),
                (MajorityElectionEndResultMockedData.CandidateId2, 150),
                (MajorityElectionEndResultMockedData.CandidateId3, 100),
                (MajorityElectionEndResultMockedData.CandidateId4, 100),
                (MajorityElectionEndResultMockedData.CandidateId5, 80),
                (MajorityElectionEndResultMockedData.CandidateId6, 70),
                (MajorityElectionEndResultMockedData.CandidateId7, 60),
                (MajorityElectionEndResultMockedData.CandidateId8, 60),
                (MajorityElectionEndResultMockedData.CandidateId9InBallotGroup, 50),
            };
        var countBySecondaryCandidateIds = new List<(string, string, int)>
            {
                (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId1, 200),
                (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId2, 100),
                (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId3, 100),
                (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId4InBallotGroup, 80),
                (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId1, 80),
                (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId2, 70),
                (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId3, 60),
                (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId4, 55),
            };
        var countOfVoters = new PoliticalBusinessCountOfVotersEventData
        {
            ConventionalReceivedBallots = 300,
            ConventionalAccountedBallots = 200,
            ConventionalBlankBallots = 50,
            ConventionalInvalidBallots = 50,
        };

        await FinishResultSubmissions(countOfVoters, countByCandidateIds, countBySecondaryCandidateIds);
        await SetOneResultToAuditedTentatively();

        var afterOneAuditedTentatively = await GetEndResult();
        afterOneAuditedTentatively.MatchSnapshot("afterOneAuditedTentatively");

        await SetOtherResultToAuditedTentatively();

        var afterAllAuditedTentatively = await GetEndResult();
        afterAllAuditedTentatively.MatchSnapshot("afterAllAuditedTentatively");
    }

    [Fact]
    public async Task TestBuildRelativeMajorityEndResult()
    {
        await SeedElection(
            MajorityElectionResultEntry.FinalResults,
            MajorityElectionMandateAlgorithm.RelativeMajority,
            3,
            1);

        await StartResultSubmissions();

        var initEndResult = await GetEndResult();
        initEndResult.MatchSnapshot("init");

        var countByCandidateIds = new List<(string, int)>
            {
                (MajorityElectionEndResultMockedData.CandidateId1, 200),
                (MajorityElectionEndResultMockedData.CandidateId2, 150),
                (MajorityElectionEndResultMockedData.CandidateId3, 100),
                (MajorityElectionEndResultMockedData.CandidateId4, 100),
                (MajorityElectionEndResultMockedData.CandidateId5, 80),
                (MajorityElectionEndResultMockedData.CandidateId6, 70),
                (MajorityElectionEndResultMockedData.CandidateId7, 60),
                (MajorityElectionEndResultMockedData.CandidateId8, 60),
                (MajorityElectionEndResultMockedData.CandidateId9InBallotGroup, 50),
            };
        var countBySecondaryCandidateIds = new List<(string, string, int)>
            {
                (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId1, 200),
                (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId2, 100),
                (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId3, 100),
                (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId4InBallotGroup, 80),
                (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId1, 80),
                (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId2, 70),
                (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId3, 60),
                (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId4, 55),
            };
        var countOfVoters = new PoliticalBusinessCountOfVotersEventData
        {
            ConventionalReceivedBallots = 300,
            ConventionalAccountedBallots = 200,
            ConventionalBlankBallots = 50,
            ConventionalInvalidBallots = 50,
        };

        await FinishResultSubmissions(
            countOfVoters,
            countByCandidateIds,
            countBySecondaryCandidateIds);

        await SetOneResultToAuditedTentatively();

        var afterOneAuditedEndResult = await GetEndResult();
        afterOneAuditedEndResult.MatchSnapshot("afterOneAuditedEndResult");

        await SetOtherResultToAuditedTentatively();

        var afterOtherAuditedEndResults = await GetEndResult();
        afterOtherAuditedEndResults.MatchSnapshot("afterOtherAuditedEndResults");
    }

    [Fact]
    public async Task TestBuildMajorityElectionEndResultWithLotDecisions()
    {
        await SeedElectionAndFinishResultSubmissions();
        await SetResultsToAuditedTentatively();
        await PublishLotDecisions();

        var endResultAfterLotDecisions = await GetEndResult();
        endResultAfterLotDecisions.CandidateEndResults.Where(x => x.Rank >= 2 && x.Rank <= 5).MatchSnapshot("after-lot-decisions-candidates");

        await ResetOneResultToSubmissionFinished(MajorityElectionEndResultMockedData.StGallenResultId);
        var endResultAfterFlaggedForCorrection = await GetEndResult();
        endResultAfterFlaggedForCorrection.CandidateEndResults.Where(x => x.Rank >= 2 && x.Rank <= 5).MatchSnapshot("after-reset");

        await SetOneResultToAuditedTentatively(MajorityElectionEndResultMockedData.StGallenResultId);
        var endResultAfterCorrectionFinished = await GetEndResult();
        endResultAfterCorrectionFinished.CandidateEndResults.Where(x => x.Rank >= 2 && x.Rank <= 5).MatchSnapshot("after-audited");

        await PublishLotDecisions();
        var endResultAfterSecondLotDecisions = await GetEndResult();
        endResultAfterSecondLotDecisions.CandidateEndResults.Where(x => x.Rank >= 2 && x.Rank <= 5).MatchSnapshot("after-second-lot-decisions-candidates");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .GetEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private GetMajorityElectionEndResultRequest NewValidRequest()
    {
        return new GetMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
        };
    }

    private async Task<ProtoModels.MajorityElectionEndResult> GetEndResult()
    {
        return await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
    }

    private async Task PublishLotDecisions()
    {
        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionEndResultLotDecisionsUpdated
            {
                MajorityElectionEndResultId = endResultId,
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                LotDecisions =
                {
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                            Rank = 4,
                        },
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.CandidateId4,
                            Rank = 3,
                        },
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                            Rank = 2,
                        },
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId3,
                            Rank = 3,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
    }
}
