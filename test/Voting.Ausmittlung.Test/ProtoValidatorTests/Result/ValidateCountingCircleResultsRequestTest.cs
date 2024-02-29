// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Result;

public class ValidateCountingCircleResultsRequestTest : ProtoValidatorBaseTest<ValidateCountingCircleResultsRequest>
{
    protected override IEnumerable<ValidateCountingCircleResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ValidateCountingCircleResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleResultIds.Add(string.Empty));
    }

    private ValidateCountingCircleResultsRequest NewValidRequest(Action<ValidateCountingCircleResultsRequest>? action = null)
    {
        var request = new ValidateCountingCircleResultsRequest
        {
            ContestId = "bad6a740-0727-4842-8c12-6d14db2c6f1e",
            CountingCircleId = "9a85696c-8296-487f-b515-83de9edbd756",
            CountingCircleResultIds =
            {
                "bbca70a9-9620-4e1a-8af3-c4b77b8f23c6",
            },
        };

        action?.Invoke(request);
        return request;
    }
}
