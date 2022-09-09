// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElection;

public class GetProportionalElectionListRequestTest : ProtoValidatorBaseTest<GetProportionalElectionListRequest>
{
    protected override IEnumerable<GetProportionalElectionListRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionListRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ListId = "invalid-guid");
        yield return NewValidRequest(x => x.ListId = string.Empty);
    }

    private GetProportionalElectionListRequest NewValidRequest(Action<GetProportionalElectionListRequest>? action = null)
    {
        var request = new GetProportionalElectionListRequest
        {
            ListId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
