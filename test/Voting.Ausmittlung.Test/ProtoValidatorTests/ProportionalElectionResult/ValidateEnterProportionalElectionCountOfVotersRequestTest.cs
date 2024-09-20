// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ValidateEnterProportionalElectionCountOfVotersRequestTest : ProtoValidatorBaseTest<ValidateEnterProportionalElectionCountOfVotersRequest>
{
    protected override IEnumerable<ValidateEnterProportionalElectionCountOfVotersRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ValidateEnterProportionalElectionCountOfVotersRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Request = null);
    }

    private ValidateEnterProportionalElectionCountOfVotersRequest NewValidRequest(Action<ValidateEnterProportionalElectionCountOfVotersRequest>? action = null)
    {
        var request = new ValidateEnterProportionalElectionCountOfVotersRequest
        {
            Request = EnterProportionalElectionCountOfVotersRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
