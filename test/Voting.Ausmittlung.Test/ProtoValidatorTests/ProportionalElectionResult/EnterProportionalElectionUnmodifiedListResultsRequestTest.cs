// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class EnterProportionalElectionUnmodifiedListResultsRequestTest : ProtoValidatorBaseTest<EnterProportionalElectionUnmodifiedListResultsRequest>
{
    protected override IEnumerable<EnterProportionalElectionUnmodifiedListResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Results.Clear());
    }

    protected override IEnumerable<EnterProportionalElectionUnmodifiedListResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private EnterProportionalElectionUnmodifiedListResultsRequest NewValidRequest(Action<EnterProportionalElectionUnmodifiedListResultsRequest>? action = null)
    {
        var request = new EnterProportionalElectionUnmodifiedListResultsRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Results =
            {
                EnterProportionalElectionUnmodifiedListResultRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
