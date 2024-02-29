// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class EnterMajorityElectionBallotGroupResultRequestTest : ProtoValidatorBaseTest<EnterMajorityElectionBallotGroupResultRequest>
{
    public static EnterMajorityElectionBallotGroupResultRequest NewValidRequest(Action<EnterMajorityElectionBallotGroupResultRequest>? action = null)
    {
        var request = new EnterMajorityElectionBallotGroupResultRequest
        {
            BallotGroupId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            VoteCount = 2,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterMajorityElectionBallotGroupResultRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.VoteCount = 0);
        yield return NewValidRequest(x => x.VoteCount = 1000000);
    }

    protected override IEnumerable<EnterMajorityElectionBallotGroupResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BallotGroupId = "invalid-guid");
        yield return NewValidRequest(x => x.BallotGroupId = string.Empty);
        yield return NewValidRequest(x => x.VoteCount = -1);
        yield return NewValidRequest(x => x.VoteCount = 1000001);
    }
}
