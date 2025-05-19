// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ResultImport;

public class ListEVotingResultImportsRequestTest : ProtoValidatorBaseTest<ListEVotingResultImportsRequest>
{
    protected override IEnumerable<ListEVotingResultImportsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListEVotingResultImportsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
    }

    private ListEVotingResultImportsRequest NewValidRequest(Action<ListEVotingResultImportsRequest>? action = null)
    {
        var request = new ListEVotingResultImportsRequest
        {
            ContestId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
        };

        action?.Invoke(request);
        return request;
    }
}
