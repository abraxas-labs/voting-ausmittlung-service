// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequestTest : ProtoValidatorBaseTest<GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest>
{
    protected override IEnumerable<GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private static GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest NewValidRequest(Action<GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest>? action = null)
    {
        var request = new GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest
        {
            ProportionalElectionId = "758eb6c9-1e7a-4695-b24a-6d4c9fc94570",
        };

        action?.Invoke(request);
        return request;
    }
}
