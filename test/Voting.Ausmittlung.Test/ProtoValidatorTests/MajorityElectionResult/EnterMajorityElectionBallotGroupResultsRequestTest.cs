// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class EnterMajorityElectionBallotGroupResultsRequestTest : ProtoValidatorBaseTest<EnterMajorityElectionBallotGroupResultsRequest>
{
    protected override IEnumerable<EnterMajorityElectionBallotGroupResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Results.Clear());
    }

    protected override IEnumerable<EnterMajorityElectionBallotGroupResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private EnterMajorityElectionBallotGroupResultsRequest NewValidRequest(Action<EnterMajorityElectionBallotGroupResultsRequest>? action = null)
    {
        var request = new EnterMajorityElectionBallotGroupResultsRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Results =
            {
                EnterMajorityElectionBallotGroupResultRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
