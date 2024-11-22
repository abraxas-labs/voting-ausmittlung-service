// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class UpdateProportionalElectionEndResultListLotDecisionRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionEndResultListLotDecisionRequest>
{
    public static UpdateProportionalElectionEndResultListLotDecisionRequest NewValidRequest(Action<UpdateProportionalElectionEndResultListLotDecisionRequest>? action = null)
    {
        var request = new UpdateProportionalElectionEndResultListLotDecisionRequest
        {
            Entries = { UpdateProportionalElectionEndResultListLotDecisionEntryRequestTest.NewValidRequest() },
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<UpdateProportionalElectionEndResultListLotDecisionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Entries.Clear());
    }

    protected override IEnumerable<UpdateProportionalElectionEndResultListLotDecisionRequest> NotOkMessages()
    {
        yield break;
    }
}
