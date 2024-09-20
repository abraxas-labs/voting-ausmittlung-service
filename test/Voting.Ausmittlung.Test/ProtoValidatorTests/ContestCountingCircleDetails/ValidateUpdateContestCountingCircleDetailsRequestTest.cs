// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ContestCountingCircleDetails;

public class ValidateUpdateContestCountingCircleDetailsRequestTest : ProtoValidatorBaseTest<ValidateUpdateContestCountingCircleDetailsRequest>
{
    protected override IEnumerable<ValidateUpdateContestCountingCircleDetailsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ValidateUpdateContestCountingCircleDetailsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Request = null);
    }

    private ValidateUpdateContestCountingCircleDetailsRequest NewValidRequest(Action<ValidateUpdateContestCountingCircleDetailsRequest>? action = null)
    {
        var request = new ValidateUpdateContestCountingCircleDetailsRequest
        {
            Request = UpdateContestCountingCircleDetailsRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
