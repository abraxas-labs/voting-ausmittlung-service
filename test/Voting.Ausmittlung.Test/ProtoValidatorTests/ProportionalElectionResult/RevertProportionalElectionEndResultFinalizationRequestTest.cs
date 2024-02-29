// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class RevertProportionalElectionEndResultFinalizationRequestTest : ProtoValidatorBaseTest<RevertProportionalElectionEndResultFinalizationRequest>
{
    protected override IEnumerable<RevertProportionalElectionEndResultFinalizationRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<RevertProportionalElectionEndResultFinalizationRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
    }

    private RevertProportionalElectionEndResultFinalizationRequest NewValidRequest(Action<RevertProportionalElectionEndResultFinalizationRequest>? action = null)
    {
        var request = new RevertProportionalElectionEndResultFinalizationRequest
        {
            ProportionalElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
