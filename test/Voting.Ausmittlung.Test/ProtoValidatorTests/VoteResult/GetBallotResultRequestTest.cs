﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class GetBallotResultRequestTest : ProtoValidatorBaseTest<GetBallotResultRequest>
{
    protected override IEnumerable<GetBallotResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetBallotResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BallotResultId = "invalid-guid");
        yield return NewValidRequest(x => x.BallotResultId = string.Empty);
    }

    private GetBallotResultRequest NewValidRequest(Action<GetBallotResultRequest>? action = null)
    {
        var request = new GetBallotResultRequest
        {
            BallotResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
