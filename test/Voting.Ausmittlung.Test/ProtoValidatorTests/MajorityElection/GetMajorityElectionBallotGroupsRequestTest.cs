// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElection;

public class GetMajorityElectionBallotGroupsRequestTest : ProtoValidatorBaseTest<GetMajorityElectionBallotGroupsRequest>
{
    protected override IEnumerable<GetMajorityElectionBallotGroupsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetMajorityElectionBallotGroupsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionId = string.Empty);
    }

    private GetMajorityElectionBallotGroupsRequest NewValidRequest(Action<GetMajorityElectionBallotGroupsRequest>? action = null)
    {
        var request = new GetMajorityElectionBallotGroupsRequest
        {
            ElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
