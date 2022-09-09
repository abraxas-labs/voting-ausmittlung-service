// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultResetToSubmissionFinishedRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultResetToSubmissionFinishedRequest>
{
    protected override IEnumerable<ProportionalElectionResultResetToSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ProportionalElectionResultResetToSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private ProportionalElectionResultResetToSubmissionFinishedRequest NewValidRequest(Action<ProportionalElectionResultResetToSubmissionFinishedRequest>? action = null)
    {
        var request = new ProportionalElectionResultResetToSubmissionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
