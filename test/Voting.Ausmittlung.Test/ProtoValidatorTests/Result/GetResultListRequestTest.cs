// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Result;

public class GetResultListRequestTest : ProtoValidatorBaseTest<GetResultListRequest>
{
    protected override IEnumerable<GetResultListRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetResultListRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
    }

    private GetResultListRequest NewValidRequest(Action<GetResultListRequest>? action = null)
    {
        var request = new GetResultListRequest
        {
            ContestId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
            CountingCircleId = "bd1167e9-0081-4472-af02-ebd799e0000c",
        };

        action?.Invoke(request);
        return request;
    }
}
