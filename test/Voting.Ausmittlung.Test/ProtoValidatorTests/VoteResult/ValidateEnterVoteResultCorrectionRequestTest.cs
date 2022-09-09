// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class ValidateEnterVoteResultCorrectionRequestTest : ProtoValidatorBaseTest<ValidateEnterVoteResultCorrectionRequest>
{
    protected override IEnumerable<ValidateEnterVoteResultCorrectionRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ValidateEnterVoteResultCorrectionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Request = null);
    }

    private ValidateEnterVoteResultCorrectionRequest NewValidRequest(Action<ValidateEnterVoteResultCorrectionRequest>? action = null)
    {
        var request = new ValidateEnterVoteResultCorrectionRequest
        {
            Request = EnterVoteResultCorrectionRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
