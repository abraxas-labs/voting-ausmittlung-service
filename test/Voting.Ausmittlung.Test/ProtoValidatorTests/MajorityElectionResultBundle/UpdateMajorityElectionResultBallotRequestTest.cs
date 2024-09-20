// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class UpdateMajorityElectionResultBallotRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionResultBallotRequest>
{
    protected override IEnumerable<UpdateMajorityElectionResultBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BallotNumber = 1);
        yield return NewValidRequest(x => x.BallotNumber = 1000000);
        yield return NewValidRequest(x => x.EmptyVoteCount = 0);
        yield return NewValidRequest(x => x.EmptyVoteCount = null);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000000);
        yield return NewValidRequest(x => x.InvalidVoteCount = 0);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000000);
        yield return NewValidRequest(x => x.IndividualVoteCount = 0);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000000);
        yield return NewValidRequest(x => x.SecondaryMajorityElectionResults.Clear());
        yield return NewValidRequest(x => x.SelectedCandidateIds.Clear());
    }

    protected override IEnumerable<UpdateMajorityElectionResultBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
        yield return NewValidRequest(x => x.BallotNumber = 0);
        yield return NewValidRequest(x => x.BallotNumber = 10000001);
        yield return NewValidRequest(x => x.EmptyVoteCount = -1);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000001);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000001);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000001);
        yield return NewValidRequest(x => x.SelectedCandidateIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.SelectedCandidateIds.Add(string.Empty));
    }

    private UpdateMajorityElectionResultBallotRequest NewValidRequest(Action<UpdateMajorityElectionResultBallotRequest>? action = null)
    {
        var request = new UpdateMajorityElectionResultBallotRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            BallotNumber = 2,
            EmptyVoteCount = 10,
            InvalidVoteCount = 1,
            IndividualVoteCount = 3,
            SecondaryMajorityElectionResults =
            {
                CreateUpdateSecondaryMajorityElectionResultBallotRequestTest.NewValidRequest(),
            },
            SelectedCandidateIds =
            {
                "2717e185-1572-4012-b07a-362256f93483",
                "6fcc8de0-a169-4add-99f8-56d7261b3036",
            },
        };

        action?.Invoke(request);
        return request;
    }
}
