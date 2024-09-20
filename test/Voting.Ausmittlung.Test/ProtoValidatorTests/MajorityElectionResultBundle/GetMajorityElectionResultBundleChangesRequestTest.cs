// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class GetMajorityElectionResultBundleChangesRequestTest : ProtoValidatorBaseTest<GetMajorityElectionResultBundleChangesRequest>
{
    protected override IEnumerable<GetMajorityElectionResultBundleChangesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetMajorityElectionResultBundleChangesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private GetMajorityElectionResultBundleChangesRequest NewValidRequest(Action<GetMajorityElectionResultBundleChangesRequest>? action = null)
    {
        var request = new GetMajorityElectionResultBundleChangesRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
