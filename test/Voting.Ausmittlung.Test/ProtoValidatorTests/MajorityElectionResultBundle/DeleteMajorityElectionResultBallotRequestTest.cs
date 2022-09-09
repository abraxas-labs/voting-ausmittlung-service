// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class DeleteMajorityElectionResultBallotRequestTest : ProtoValidatorBaseTest<DeleteMajorityElectionResultBallotRequest>
{
    protected override IEnumerable<DeleteMajorityElectionResultBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BallotNumber = 1);
        yield return NewValidRequest(x => x.BallotNumber = 1000000);
    }

    protected override IEnumerable<DeleteMajorityElectionResultBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
        yield return NewValidRequest(x => x.BallotNumber = 0);
        yield return NewValidRequest(x => x.BallotNumber = 10000001);
    }

    private DeleteMajorityElectionResultBallotRequest NewValidRequest(Action<DeleteMajorityElectionResultBallotRequest>? action = null)
    {
        var request = new DeleteMajorityElectionResultBallotRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            BallotNumber = 4,
        };

        action?.Invoke(request);
        return request;
    }
}
