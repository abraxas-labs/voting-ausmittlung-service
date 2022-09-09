// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class GetMajorityElectionResultRequestTest : ProtoValidatorBaseTest<GetMajorityElectionResultRequest>
{
    protected override IEnumerable<GetMajorityElectionResultRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.ElectionId = string.Empty);
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    protected override IEnumerable<GetMajorityElectionResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
    }

    private GetMajorityElectionResultRequest NewValidRequest(Action<GetMajorityElectionResultRequest>? action = null)
    {
        var request = new GetMajorityElectionResultRequest
        {
            CountingCircleId = "02f3f108-1b8c-4a01-8471-9e12f07b7514",
            ElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            ElectionResultId = "d033decb-0429-43dd-8671-aca3e45b646b",
        };

        action?.Invoke(request);
        return request;
    }
}
