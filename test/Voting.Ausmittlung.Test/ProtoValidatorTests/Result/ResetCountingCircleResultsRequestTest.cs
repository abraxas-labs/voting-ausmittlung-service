// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Result;

public class ResetCountingCircleResultsRequestTest : ProtoValidatorBaseTest<ResetCountingCircleResultsRequest>
{
    protected override IEnumerable<ResetCountingCircleResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ResetCountingCircleResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
    }

    private ResetCountingCircleResultsRequest NewValidRequest(Action<ResetCountingCircleResultsRequest>? action = null)
    {
        var request = new ResetCountingCircleResultsRequest
        {
            ContestId = "bad6a740-0727-4842-8c12-6d14db2c6f1e",
            CountingCircleId = "9a85696c-8296-487f-b515-83de9edbd756",
        };

        action?.Invoke(request);
        return request;
    }
}
