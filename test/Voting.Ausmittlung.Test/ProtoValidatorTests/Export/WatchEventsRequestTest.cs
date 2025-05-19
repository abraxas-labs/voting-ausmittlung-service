// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class WatchEventsRequestTest : ProtoValidatorBaseTest<WatchEventsRequest>
{
    protected override IEnumerable<WatchEventsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.Filters[0].Id = "fooBar");
        yield return NewValidRequest(x => x.Filters[0].PoliticalBusinessId = string.Empty);
        yield return NewValidRequest(x => x.Filters[0].PoliticalBusinessResultId = string.Empty);
        yield return NewValidRequest(x => x.Filters.Clear());
    }

    protected override IEnumerable<WatchEventsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.Filters[0].PoliticalBusinessId = "invalid-guid");
        yield return NewValidRequest(x => x.Filters[0].PoliticalBusinessResultId = "invalid-guid");
    }

    private WatchEventsRequest NewValidRequest(Action<WatchEventsRequest>? action = null)
    {
        var request = new WatchEventsRequest
        {
            ContestId = "e2330cc1-39c3-4db4-b73c-ad0e54994d04",
            CountingCircleId = "ba1cd0d8-99aa-433e-9887-3e4bba46fbac",
            Filters =
            {
                new WatchEventsRequestFilter
                {
                    PoliticalBusinessResultId = "69ec0926-b324-45a7-8efb-34c36e2eb7b8",
                    PoliticalBusinessId = "da4be85f-8fb0-4b19-b4d9-b0aeb39122e6",
                    Id = "475b406e-140e-4514-abae-08263fa939a6",
                    Types_ =
                    {
                        "fooBar",
                    },
                },
            },
        };

        action?.Invoke(request);
        return request;
    }
}
