// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResultBundle;

public class GetProportionalElectionResultBundleChangesRequestTest : ProtoValidatorBaseTest<GetProportionalElectionResultBundleChangesRequest>
{
    protected override IEnumerable<GetProportionalElectionResultBundleChangesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetProportionalElectionResultBundleChangesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
    }

    private GetProportionalElectionResultBundleChangesRequest NewValidRequest(Action<GetProportionalElectionResultBundleChangesRequest>? action = null)
    {
        var request = new GetProportionalElectionResultBundleChangesRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
