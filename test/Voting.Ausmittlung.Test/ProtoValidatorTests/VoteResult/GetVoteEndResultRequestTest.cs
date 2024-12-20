﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class GetVoteEndResultRequestTest : ProtoValidatorBaseTest<GetVoteEndResultRequest>
{
    protected override IEnumerable<GetVoteEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetVoteEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteId = string.Empty);
    }

    private GetVoteEndResultRequest NewValidRequest(Action<GetVoteEndResultRequest>? action = null)
    {
        var request = new GetVoteEndResultRequest
        {
            VoteId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
