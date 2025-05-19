// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ResultImport;

public class ListECountingResultImportsRequestTest : ProtoValidatorBaseTest<ListECountingResultImportsRequest>
{
    protected override IEnumerable<ListECountingResultImportsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListECountingResultImportsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
    }

    private ListECountingResultImportsRequest NewValidRequest(Action<ListECountingResultImportsRequest>? action = null)
    {
        var request = new ListECountingResultImportsRequest
        {
            ContestId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
            CountingCircleId = "9b309d30-4793-486c-835a-9a3915da41f7",
        };

        action?.Invoke(request);
        return request;
    }
}
