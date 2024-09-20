﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class MajorityElectionResultPrepareSubmissionFinishedRequestTest : ProtoValidatorBaseTest<MajorityElectionResultPrepareSubmissionFinishedRequest>
{
    protected override IEnumerable<MajorityElectionResultPrepareSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<MajorityElectionResultPrepareSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private MajorityElectionResultPrepareSubmissionFinishedRequest NewValidRequest(Action<MajorityElectionResultPrepareSubmissionFinishedRequest>? action = null)
    {
        var request = new MajorityElectionResultPrepareSubmissionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
