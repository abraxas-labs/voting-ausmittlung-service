// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class GetProportionalElectionListEndResultAvailableLotDecisionsRequestTest : ProtoValidatorBaseTest<GetProportionalElectionListEndResultAvailableLotDecisionsRequest>
{
    protected override IEnumerable<GetProportionalElectionListEndResultAvailableLotDecisionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionListEndResultAvailableLotDecisionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionListId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionListId = string.Empty);
    }

    private GetProportionalElectionListEndResultAvailableLotDecisionsRequest NewValidRequest(Action<GetProportionalElectionListEndResultAvailableLotDecisionsRequest>? action = null)
    {
        var request = new GetProportionalElectionListEndResultAvailableLotDecisionsRequest
        {
            ProportionalElectionListId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
