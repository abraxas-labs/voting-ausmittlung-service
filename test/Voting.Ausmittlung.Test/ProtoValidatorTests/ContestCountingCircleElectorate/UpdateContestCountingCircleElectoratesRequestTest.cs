// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ContestCountingCircleElectorate;

public class UpdateContestCountingCircleElectoratesRequestTest : ProtoValidatorBaseTest<UpdateContestCountingCircleElectoratesRequest>
{
    protected override IEnumerable<UpdateContestCountingCircleElectoratesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<UpdateContestCountingCircleElectoratesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
    }

    private static UpdateContestCountingCircleElectoratesRequest NewValidRequest(Action<UpdateContestCountingCircleElectoratesRequest>? action = null)
    {
        var request = new UpdateContestCountingCircleElectoratesRequest
        {
            ContestId = "dc10df46-0fd9-4e50-a8f0-b506af45a6df",
            CountingCircleId = "3c56e92d-bd4d-4af8-b414-098b87e029af",
            Electorates = { CreateUpdateContestCountingCircleElectorateRequestTest.NewValidRequest() },
        };

        action?.Invoke(request);
        return request;
    }
}
