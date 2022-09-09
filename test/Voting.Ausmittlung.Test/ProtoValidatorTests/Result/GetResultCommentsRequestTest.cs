// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Result;

public class GetResultCommentsRequestTest : ProtoValidatorBaseTest<GetResultCommentsRequest>
{
    protected override IEnumerable<GetResultCommentsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetResultCommentsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ResultId = string.Empty);
    }

    private GetResultCommentsRequest NewValidRequest(Action<GetResultCommentsRequest>? action = null)
    {
        var request = new GetResultCommentsRequest
        {
            ResultId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
        };

        action?.Invoke(request);
        return request;
    }
}
