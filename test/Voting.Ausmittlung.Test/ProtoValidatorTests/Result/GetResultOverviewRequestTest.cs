// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Result;

public class GetResultOverviewRequestTest : ProtoValidatorBaseTest<GetResultOverviewRequest>
{
    protected override IEnumerable<GetResultOverviewRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetResultOverviewRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
    }

    private GetResultOverviewRequest NewValidRequest(Action<GetResultOverviewRequest>? action = null)
    {
        var request = new GetResultOverviewRequest
        {
            ContestId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
        };

        action?.Invoke(request);
        return request;
    }
}
