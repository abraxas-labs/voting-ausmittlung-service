﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class MajorityElectionResultResetToSubmissionFinishedRequestTest : ProtoValidatorBaseTest<MajorityElectionResultResetToSubmissionFinishedRequest>
{
    protected override IEnumerable<MajorityElectionResultResetToSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<MajorityElectionResultResetToSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private MajorityElectionResultResetToSubmissionFinishedRequest NewValidRequest(Action<MajorityElectionResultResetToSubmissionFinishedRequest>? action = null)
    {
        var request = new MajorityElectionResultResetToSubmissionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
