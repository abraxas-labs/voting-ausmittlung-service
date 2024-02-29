// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class GetMajorityElectionResultBundlesRequestTest : ProtoValidatorBaseTest<GetMajorityElectionResultBundlesRequest>
{
    protected override IEnumerable<GetMajorityElectionResultBundlesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetMajorityElectionResultBundlesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private GetMajorityElectionResultBundlesRequest NewValidRequest(Action<GetMajorityElectionResultBundlesRequest>? action = null)
    {
        var request = new GetMajorityElectionResultBundlesRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
