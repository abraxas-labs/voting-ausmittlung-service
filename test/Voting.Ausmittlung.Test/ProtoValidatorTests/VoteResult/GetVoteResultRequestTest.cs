// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class GetVoteResultRequestTest : ProtoValidatorBaseTest<GetVoteResultRequest>
{
    protected override IEnumerable<GetVoteResultRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.VoteId = string.Empty);
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
    }

    protected override IEnumerable<GetVoteResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
    }

    private GetVoteResultRequest NewValidRequest(Action<GetVoteResultRequest>? action = null)
    {
        var request = new GetVoteResultRequest
        {
            CountingCircleId = "02f3f108-1b8c-4a01-8471-9e12f07b7514",
            VoteId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            VoteResultId = "d033decb-0429-43dd-8671-aca3e45b646b",
        };

        action?.Invoke(request);
        return request;
    }
}
