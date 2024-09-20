// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ContestCountingCircleDetails;

public class UpdateContestCountingCircleDetailsRequestTest : ProtoValidatorBaseTest<UpdateContestCountingCircleDetailsRequest>
{
    public static UpdateContestCountingCircleDetailsRequest NewValidRequest(Action<UpdateContestCountingCircleDetailsRequest>? action = null)
    {
        var request = new UpdateContestCountingCircleDetailsRequest
        {
            ContestId = "dc10df46-0fd9-4e50-a8f0-b506af45a6df",
            CountingCircleId = "3c56e92d-bd4d-4af8-b414-098b87e029af",
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<UpdateContestCountingCircleDetailsRequest> OkMessages()
    {
        // TODO: With repeated Childs
        yield return NewValidRequest();
    }

    protected override IEnumerable<UpdateContestCountingCircleDetailsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
    }
}
