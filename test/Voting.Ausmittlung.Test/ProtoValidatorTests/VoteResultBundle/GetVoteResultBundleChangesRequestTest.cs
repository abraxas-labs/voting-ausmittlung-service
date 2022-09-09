// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResultBundle;

public class GetVoteResultBundleChangesRequestTest : ProtoValidatorBaseTest<GetVoteResultBundleChangesRequest>
{
    protected override IEnumerable<GetVoteResultBundleChangesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetVoteResultBundleChangesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BallotResultId = "invalid-guid");
        yield return NewValidRequest(x => x.BallotResultId = string.Empty);
    }

    private GetVoteResultBundleChangesRequest NewValidRequest(Action<GetVoteResultBundleChangesRequest>? action = null)
    {
        var request = new GetVoteResultBundleChangesRequest
        {
            BallotResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
