// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionUnionResult;

public class GetProportionalElectionUnionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequestTest : ProtoValidatorBaseTest<GetProportionalElectionUnionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest>
{
    protected override IEnumerable<GetProportionalElectionUnionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionUnionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
    }

    private static GetProportionalElectionUnionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest NewValidRequest(Action<GetProportionalElectionUnionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest>? action = null)
    {
        var request = new GetProportionalElectionUnionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest
        {
            ProportionalElectionUnionId = "758eb6c9-1e7a-4695-b24a-6d4c9fc94570",
        };

        action?.Invoke(request);
        return request;
    }
}
