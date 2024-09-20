// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class UpdateProportionalElectionListEndResultLotDecisionsRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionListEndResultLotDecisionsRequest>
{
    protected override IEnumerable<UpdateProportionalElectionListEndResultLotDecisionsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.LotDecisions.Clear());
    }

    protected override IEnumerable<UpdateProportionalElectionListEndResultLotDecisionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionListId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionListId = string.Empty);
    }

    private UpdateProportionalElectionListEndResultLotDecisionsRequest NewValidRequest(Action<UpdateProportionalElectionListEndResultLotDecisionsRequest>? action = null)
    {
        var request = new UpdateProportionalElectionListEndResultLotDecisionsRequest
        {
            ProportionalElectionListId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            LotDecisions =
            {
                UpdateProportionalElectionEndResultLotDecisionRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
