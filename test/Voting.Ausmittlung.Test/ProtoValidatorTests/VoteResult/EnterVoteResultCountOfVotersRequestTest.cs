// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class EnterVoteResultCountOfVotersRequestTest : ProtoValidatorBaseTest<EnterVoteResultCountOfVotersRequest>
{
    public static EnterVoteResultCountOfVotersRequest NewValidRequest(Action<EnterVoteResultCountOfVotersRequest>? action = null)
    {
        var request = new EnterVoteResultCountOfVotersRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            ResultsCountOfVoters =
            {
                EnterVoteBallotResultsCountOfVotersRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterVoteResultCountOfVotersRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ResultsCountOfVoters.Clear());
    }

    protected override IEnumerable<EnterVoteResultCountOfVotersRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
    }
}
