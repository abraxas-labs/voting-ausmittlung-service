// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class ValidateEnterMajorityElectionCountOfVotersRequestTest : ProtoValidatorBaseTest<ValidateEnterMajorityElectionCountOfVotersRequest>
{
    protected override IEnumerable<ValidateEnterMajorityElectionCountOfVotersRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ValidateEnterMajorityElectionCountOfVotersRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Request = null);
    }

    private ValidateEnterMajorityElectionCountOfVotersRequest NewValidRequest(Action<ValidateEnterMajorityElectionCountOfVotersRequest>? action = null)
    {
        var request = new ValidateEnterMajorityElectionCountOfVotersRequest
        {
            Request = EnterMajorityElectionCountOfVotersRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
