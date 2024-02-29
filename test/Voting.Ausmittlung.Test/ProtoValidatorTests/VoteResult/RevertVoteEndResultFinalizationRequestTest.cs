// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class RevertVoteEndResultFinalizationRequestTest : ProtoValidatorBaseTest<RevertVoteEndResultFinalizationRequest>
{
    protected override IEnumerable<RevertVoteEndResultFinalizationRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<RevertVoteEndResultFinalizationRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteId = string.Empty);
    }

    private RevertVoteEndResultFinalizationRequest NewValidRequest(Action<RevertVoteEndResultFinalizationRequest>? action = null)
    {
        var request = new RevertVoteEndResultFinalizationRequest
        {
            VoteId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
