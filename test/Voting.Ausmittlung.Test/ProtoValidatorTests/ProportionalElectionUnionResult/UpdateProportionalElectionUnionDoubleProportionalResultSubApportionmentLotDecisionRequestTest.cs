// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionUnionResult;

public class UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest>
{
    protected override IEnumerable<UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
        yield return NewValidRequest(x => x.Number = 0);
        yield return NewValidRequest(x => x.Number = 101);
    }

    private static UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest NewValidRequest(Action<UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest>? action = null)
    {
        var request = new UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest
        {
            ProportionalElectionUnionId = "758eb6c9-1e7a-4695-b24a-6d4c9fc94570",
            Number = 1,
        };

        action?.Invoke(request);
        return request;
    }
}
