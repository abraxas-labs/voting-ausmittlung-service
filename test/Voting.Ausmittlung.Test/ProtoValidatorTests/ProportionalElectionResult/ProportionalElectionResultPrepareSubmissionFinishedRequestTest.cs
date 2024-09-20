// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultPrepareSubmissionFinishedRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultPrepareSubmissionFinishedRequest>
{
    protected override IEnumerable<ProportionalElectionResultPrepareSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ProportionalElectionResultPrepareSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private ProportionalElectionResultPrepareSubmissionFinishedRequest NewValidRequest(Action<ProportionalElectionResultPrepareSubmissionFinishedRequest>? action = null)
    {
        var request = new ProportionalElectionResultPrepareSubmissionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
