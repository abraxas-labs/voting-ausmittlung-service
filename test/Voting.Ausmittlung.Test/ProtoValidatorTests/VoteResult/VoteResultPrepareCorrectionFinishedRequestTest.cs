// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class VoteResultPrepareCorrectionFinishedRequestTest : ProtoValidatorBaseTest<VoteResultPrepareCorrectionFinishedRequest>
{
    protected override IEnumerable<VoteResultPrepareCorrectionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<VoteResultPrepareCorrectionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
    }

    private VoteResultPrepareCorrectionFinishedRequest NewValidRequest(Action<VoteResultPrepareCorrectionFinishedRequest>? action = null)
    {
        var request = new VoteResultPrepareCorrectionFinishedRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
