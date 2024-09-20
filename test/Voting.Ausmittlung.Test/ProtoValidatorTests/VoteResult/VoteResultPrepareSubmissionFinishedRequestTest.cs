// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class VoteResultPrepareSubmissionFinishedRequestTest : ProtoValidatorBaseTest<VoteResultPrepareSubmissionFinishedRequest>
{
    protected override IEnumerable<VoteResultPrepareSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<VoteResultPrepareSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
    }

    private VoteResultPrepareSubmissionFinishedRequest NewValidRequest(Action<VoteResultPrepareSubmissionFinishedRequest>? action = null)
    {
        var request = new VoteResultPrepareSubmissionFinishedRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
