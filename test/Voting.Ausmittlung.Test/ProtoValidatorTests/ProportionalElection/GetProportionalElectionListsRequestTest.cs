// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElection;

public class GetProportionalElectionListsRequestTest : ProtoValidatorBaseTest<GetProportionalElectionListsRequest>
{
    protected override IEnumerable<GetProportionalElectionListsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionListsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionId = string.Empty);
    }

    private GetProportionalElectionListsRequest NewValidRequest(Action<GetProportionalElectionListsRequest>? action = null)
    {
        var request = new GetProportionalElectionListsRequest
        {
            ElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
