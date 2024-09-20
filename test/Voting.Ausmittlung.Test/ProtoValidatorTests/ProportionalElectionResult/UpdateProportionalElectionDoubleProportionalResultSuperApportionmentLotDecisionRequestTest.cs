// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest>
{
    protected override IEnumerable<UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
        yield return NewValidRequest(x => x.Number = 0);
        yield return NewValidRequest(x => x.Number = 101);
    }

    private static UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest NewValidRequest(Action<UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest>? action = null)
    {
        var request = new UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest
        {
            ProportionalElectionId = "758eb6c9-1e7a-4695-b24a-6d4c9fc94570",
            Number = 1,
        };

        action?.Invoke(request);
        return request;
    }
}
