// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class VoteResultResetToSubmissionFinishedRequestTest : ProtoValidatorBaseTest<VoteResultResetToSubmissionFinishedRequest>
{
    protected override IEnumerable<VoteResultResetToSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<VoteResultResetToSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
    }

    private VoteResultResetToSubmissionFinishedRequest NewValidRequest(Action<VoteResultResetToSubmissionFinishedRequest>? action = null)
    {
        var request = new VoteResultResetToSubmissionFinishedRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
