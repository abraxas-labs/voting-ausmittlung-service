// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class CreateMajorityElectionResultBallotRequestTest : ProtoValidatorBaseTest<CreateMajorityElectionResultBallotRequest>
{
    protected override IEnumerable<CreateMajorityElectionResultBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.EmptyVoteCount = 0);
        yield return NewValidRequest(x => x.EmptyVoteCount = null);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000000);
        yield return NewValidRequest(x => x.InvalidVoteCount = 0);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000000);
        yield return NewValidRequest(x => x.IndividualVoteCount = 0);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000000);
        yield return NewValidRequest(x => x.SecondaryMajorityElectionResults.Clear());
        yield return NewValidRequest(x => x.Candidates.Clear());
    }

    protected override IEnumerable<CreateMajorityElectionResultBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
        yield return NewValidRequest(x => x.EmptyVoteCount = -1);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000001);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000001);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000001);
    }

    private CreateMajorityElectionResultBallotRequest NewValidRequest(Action<CreateMajorityElectionResultBallotRequest>? action = null)
    {
        var request = new CreateMajorityElectionResultBallotRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            EmptyVoteCount = 10,
            InvalidVoteCount = 1,
            IndividualVoteCount = 3,
            SecondaryMajorityElectionResults =
            {
                CreateUpdateSecondaryMajorityElectionResultBallotRequestTest.NewValidRequest(),
            },
            Candidates =
            {
                CreateUpdateMajorityElectionResultBallotCandidateRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
