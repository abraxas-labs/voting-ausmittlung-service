// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultsPlausibiliseRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultsPlausibiliseRequest>
{
    protected override IEnumerable<ProportionalElectionResultsPlausibiliseRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ElectionResultIds.Clear());
    }

    protected override IEnumerable<ProportionalElectionResultsPlausibiliseRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.ElectionResultIds.Add(string.Empty));
    }

    private ProportionalElectionResultsPlausibiliseRequest NewValidRequest(Action<ProportionalElectionResultsPlausibiliseRequest>? action = null)
    {
        var request = new ProportionalElectionResultsPlausibiliseRequest
        {
            ElectionResultIds =
            {
                "f67b688a-0566-4e3c-bd73-6063834fedaf",
                "87a982e0-b8df-4318-b787-28a0c01693a4",
            },
        };

        action?.Invoke(request);
        return request;
    }
}
