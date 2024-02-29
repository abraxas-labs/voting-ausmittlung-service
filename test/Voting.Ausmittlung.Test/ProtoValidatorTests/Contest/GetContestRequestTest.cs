// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Contest;

public class GetContestRequestTest : ProtoValidatorBaseTest<GetContestRequest>
{
    protected override IEnumerable<GetContestRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetContestRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.Id = string.Empty);
    }

    private GetContestRequest NewValidRequest(Action<GetContestRequest>? action = null)
    {
        var request = new GetContestRequest
        {
            Id = "04a2aff6-240f-4496-9e97-29881e84a2d4",
        };

        action?.Invoke(request);
        return request;
    }
}
