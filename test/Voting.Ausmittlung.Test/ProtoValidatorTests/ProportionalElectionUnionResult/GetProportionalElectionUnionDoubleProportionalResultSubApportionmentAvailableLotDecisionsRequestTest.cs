// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionUnionResult;

public class GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequestTest : ProtoValidatorBaseTest<GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest>
{
    protected override IEnumerable<GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
    }

    private static GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest NewValidRequest(Action<GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest>? action = null)
    {
        var request = new GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest
        {
            ProportionalElectionUnionId = "758eb6c9-1e7a-4695-b24a-6d4c9fc94570",
        };

        action?.Invoke(request);
        return request;
    }
}
