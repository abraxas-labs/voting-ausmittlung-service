// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class EnterProportionalElectionUnmodifiedListResultRequestTest : ProtoValidatorBaseTest<EnterProportionalElectionUnmodifiedListResultRequest>
{
    public static EnterProportionalElectionUnmodifiedListResultRequest NewValidRequest(Action<EnterProportionalElectionUnmodifiedListResultRequest>? action = null)
    {
        var request = new EnterProportionalElectionUnmodifiedListResultRequest
        {
            ListId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            VoteCount = 100,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterProportionalElectionUnmodifiedListResultRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.VoteCount = 0);
        yield return NewValidRequest(x => x.VoteCount = 1000000);
    }

    protected override IEnumerable<EnterProportionalElectionUnmodifiedListResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ListId = "invalid-guid");
        yield return NewValidRequest(x => x.ListId = string.Empty);
        yield return NewValidRequest(x => x.VoteCount = -1);
        yield return NewValidRequest(x => x.VoteCount = 1000001);
    }
}
