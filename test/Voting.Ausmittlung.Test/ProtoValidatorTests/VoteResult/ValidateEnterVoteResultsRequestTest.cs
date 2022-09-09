// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class ValidateEnterVoteResultsRequestTest : ProtoValidatorBaseTest<ValidateEnterVoteResultsRequest>
{
    protected override IEnumerable<ValidateEnterVoteResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ValidateEnterVoteResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Request = null);
    }

    private ValidateEnterVoteResultsRequest NewValidRequest(Action<ValidateEnterVoteResultsRequest>? action = null)
    {
        var request = new ValidateEnterVoteResultsRequest
        {
            Request = EnterVoteResultsRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
