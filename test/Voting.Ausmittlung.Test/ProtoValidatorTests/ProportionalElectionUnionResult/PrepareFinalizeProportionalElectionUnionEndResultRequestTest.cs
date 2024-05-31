// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionUnionResult;

public class PrepareFinalizeProportionalElectionUnionEndResultRequestTest : ProtoValidatorBaseTest<PrepareFinalizeProportionalElectionUnionEndResultRequest>
{
    protected override IEnumerable<PrepareFinalizeProportionalElectionUnionEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<PrepareFinalizeProportionalElectionUnionEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
    }

    private PrepareFinalizeProportionalElectionUnionEndResultRequest NewValidRequest(Action<PrepareFinalizeProportionalElectionUnionEndResultRequest>? action = null)
    {
        var request = new PrepareFinalizeProportionalElectionUnionEndResultRequest
        {
            ProportionalElectionUnionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
