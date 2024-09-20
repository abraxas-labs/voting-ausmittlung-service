// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.ProtoValidatorTests.CountOfVoters;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class EnterVoteBallotResultsCountOfVotersRequestTest : ProtoValidatorBaseTest<EnterVoteBallotResultsCountOfVotersRequest>
{
    public static EnterVoteBallotResultsCountOfVotersRequest NewValidRequest(Action<EnterVoteBallotResultsCountOfVotersRequest>? action = null)
    {
        var request = new EnterVoteBallotResultsCountOfVotersRequest
        {
            BallotId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            CountOfVoters = EnterPoliticalBusinessCountOfVotersRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterVoteBallotResultsCountOfVotersRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<EnterVoteBallotResultsCountOfVotersRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BallotId = "invalid-guid");
        yield return NewValidRequest(x => x.BallotId = string.Empty);
        yield return NewValidRequest(x => x.CountOfVoters = null);
    }
}
