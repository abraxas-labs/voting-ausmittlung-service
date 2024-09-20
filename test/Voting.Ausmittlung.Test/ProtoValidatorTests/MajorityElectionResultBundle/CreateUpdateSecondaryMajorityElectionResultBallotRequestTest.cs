// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class CreateUpdateSecondaryMajorityElectionResultBallotRequestTest : ProtoValidatorBaseTest<CreateUpdateSecondaryMajorityElectionResultBallotRequest>
{
    public static CreateUpdateSecondaryMajorityElectionResultBallotRequest NewValidRequest(Action<CreateUpdateSecondaryMajorityElectionResultBallotRequest>? action = null)
    {
        var request = new CreateUpdateSecondaryMajorityElectionResultBallotRequest
        {
            SecondaryMajorityElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SelectedCandidateIds =
            {
                "2af9848b-4f3a-4d5f-8696-3e059d1a2ba2",
                "1927cf23-7a87-4c81-a031-807680639746",
            },
            EmptyVoteCount = 4,
            IndividualVoteCount = 3,
            InvalidVoteCount = 2,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<CreateUpdateSecondaryMajorityElectionResultBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.EmptyVoteCount = 0);
        yield return NewValidRequest(x => x.EmptyVoteCount = null);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000000);
        yield return NewValidRequest(x => x.InvalidVoteCount = 0);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000000);
        yield return NewValidRequest(x => x.IndividualVoteCount = 0);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000000);
    }

    protected override IEnumerable<CreateUpdateSecondaryMajorityElectionResultBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = string.Empty);
        yield return NewValidRequest(x => x.SelectedCandidateIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.SelectedCandidateIds.Add(string.Empty));
        yield return NewValidRequest(x => x.EmptyVoteCount = -1);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000001);
        yield return NewValidRequest(x => x.InvalidVoteCount = -1);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000001);
        yield return NewValidRequest(x => x.IndividualVoteCount = -1);
        yield return NewValidRequest(x => x.IndividualVoteCount = 10000001);
    }
}
