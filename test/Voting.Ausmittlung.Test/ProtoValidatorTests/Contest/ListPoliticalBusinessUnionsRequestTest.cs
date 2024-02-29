// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Contest;

public class ListPoliticalBusinessUnionsRequestTest : ProtoValidatorBaseTest<ListPoliticalBusinessUnionsRequest>
{
    protected override IEnumerable<ListPoliticalBusinessUnionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListPoliticalBusinessUnionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
    }

    private ListPoliticalBusinessUnionsRequest NewValidRequest(Action<ListPoliticalBusinessUnionsRequest>? action = null)
    {
        var request = new ListPoliticalBusinessUnionsRequest
        {
            ContestId = "624e7d0a-0d87-4721-87f5-c7a73001a551",
        };

        action?.Invoke(request);
        return request;
    }
}
