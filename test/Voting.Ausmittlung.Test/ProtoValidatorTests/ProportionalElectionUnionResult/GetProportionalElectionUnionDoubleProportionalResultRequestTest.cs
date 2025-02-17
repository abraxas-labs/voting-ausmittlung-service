﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionUnionResult;

public class GetProportionalElectionUnionDoubleProportionalResultRequestTest : ProtoValidatorBaseTest<GetProportionalElectionUnionDoubleProportionalResultRequest>
{
    protected override IEnumerable<GetProportionalElectionUnionDoubleProportionalResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionUnionDoubleProportionalResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
    }

    private GetProportionalElectionUnionDoubleProportionalResultRequest NewValidRequest(Action<GetProportionalElectionUnionDoubleProportionalResultRequest>? action = null)
    {
        var request = new GetProportionalElectionUnionDoubleProportionalResultRequest
        {
            ProportionalElectionUnionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
