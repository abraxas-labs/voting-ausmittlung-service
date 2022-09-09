// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class ValidateEnterVoteResultCountOfVotersRequestTest : ProtoValidatorBaseTest<ValidateEnterVoteResultCountOfVotersRequest>
{
    protected override IEnumerable<ValidateEnterVoteResultCountOfVotersRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ValidateEnterVoteResultCountOfVotersRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Request = null);
    }

    private ValidateEnterVoteResultCountOfVotersRequest NewValidRequest(Action<ValidateEnterVoteResultCountOfVotersRequest>? action = null)
    {
        var request = new ValidateEnterVoteResultCountOfVotersRequest
        {
            Request = EnterVoteResultCountOfVotersRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
