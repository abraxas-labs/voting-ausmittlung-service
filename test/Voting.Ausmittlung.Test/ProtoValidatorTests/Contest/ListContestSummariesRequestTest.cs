// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Contest;

public class ListContestSummariesRequestTest : ProtoValidatorBaseTest<ListContestSummariesRequest>
{
    protected override IEnumerable<ListContestSummariesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.States.Clear());
    }

    protected override IEnumerable<ListContestSummariesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x =>
        {
            x.States.Clear();
            x.States.Add(ContestState.Unspecified);
        });

        yield return NewValidRequest(x =>
        {
            x.States.Clear();
            x.States.Add((ContestState)(-1));
        });
    }

    private ListContestSummariesRequest NewValidRequest(Action<ListContestSummariesRequest>? action = null)
    {
        var request = new ListContestSummariesRequest
        {
            States =
            {
                ContestState.Active,
                ContestState.Archived,
            },
        };

        action?.Invoke(request);
        return request;
    }
}
