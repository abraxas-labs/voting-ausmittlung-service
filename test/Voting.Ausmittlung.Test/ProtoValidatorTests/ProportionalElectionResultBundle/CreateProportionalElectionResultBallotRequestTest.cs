// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResultBundle;

public class CreateProportionalElectionResultBallotRequestTest : ProtoValidatorBaseTest<CreateProportionalElectionResultBallotRequest>
{
    protected override IEnumerable<CreateProportionalElectionResultBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.EmptyVoteCount = 0);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000000);
        yield return NewValidRequest(x => x.Candidates.Clear());
    }

    protected override IEnumerable<CreateProportionalElectionResultBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
        yield return NewValidRequest(x => x.EmptyVoteCount = -1);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000001);
    }

    private CreateProportionalElectionResultBallotRequest NewValidRequest(Action<CreateProportionalElectionResultBallotRequest>? action = null)
    {
        var request = new CreateProportionalElectionResultBallotRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            EmptyVoteCount = 10,
            Candidates =
            {
                CreateUpdateProportionalElectionResultBallotCandidateRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
