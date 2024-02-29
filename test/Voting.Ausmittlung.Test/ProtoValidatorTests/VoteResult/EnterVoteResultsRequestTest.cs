// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class EnterVoteResultsRequestTest : ProtoValidatorBaseTest<EnterVoteResultsRequest>
{
    public static EnterVoteResultsRequest NewValidRequest(Action<EnterVoteResultsRequest>? action = null)
    {
        var request = new EnterVoteResultsRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Results =
            {
                EnterVoteBallotResultsRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterVoteResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Results.Clear());
    }

    protected override IEnumerable<EnterVoteResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
    }
}
