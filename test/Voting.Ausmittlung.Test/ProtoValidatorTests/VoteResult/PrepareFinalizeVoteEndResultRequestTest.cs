// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class PrepareFinalizeVoteEndResultRequestTest : ProtoValidatorBaseTest<PrepareFinalizeVoteEndResultRequest>
{
    protected override IEnumerable<PrepareFinalizeVoteEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<PrepareFinalizeVoteEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteId = string.Empty);
    }

    private PrepareFinalizeVoteEndResultRequest NewValidRequest(Action<PrepareFinalizeVoteEndResultRequest>? action = null)
    {
        var request = new PrepareFinalizeVoteEndResultRequest
        {
            VoteId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
