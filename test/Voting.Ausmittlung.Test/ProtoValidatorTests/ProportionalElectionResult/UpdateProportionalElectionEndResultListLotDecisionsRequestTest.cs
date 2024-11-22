// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class UpdateProportionalElectionEndResultListLotDecisionsRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionEndResultListLotDecisionsRequest>
{
    protected override IEnumerable<UpdateProportionalElectionEndResultListLotDecisionsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ListLotDecisions.Clear());
    }

    protected override IEnumerable<UpdateProportionalElectionEndResultListLotDecisionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private UpdateProportionalElectionEndResultListLotDecisionsRequest NewValidRequest(Action<UpdateProportionalElectionEndResultListLotDecisionsRequest>? action = null)
    {
        var request = new UpdateProportionalElectionEndResultListLotDecisionsRequest
        {
            ProportionalElectionId = "c7e286a2-677d-4b87-9ec6-61f9176196a8",
            ListLotDecisions = { UpdateProportionalElectionEndResultListLotDecisionRequestTest.NewValidRequest() },
        };

        action?.Invoke(request);
        return request;
    }
}
