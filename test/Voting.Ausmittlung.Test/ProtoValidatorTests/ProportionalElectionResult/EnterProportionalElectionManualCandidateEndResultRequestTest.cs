// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class EnterProportionalElectionManualCandidateEndResultRequestTest : ProtoValidatorBaseTest<EnterProportionalElectionManualCandidateEndResultRequest>
{
    internal static EnterProportionalElectionManualCandidateEndResultRequest NewValidRequest(Action<EnterProportionalElectionManualCandidateEndResultRequest>? action = null)
    {
        var request = new EnterProportionalElectionManualCandidateEndResultRequest
        {
            CandidateId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            State = ProportionalElectionCandidateEndResultState.Elected,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterProportionalElectionManualCandidateEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<EnterProportionalElectionManualCandidateEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CandidateId = "invalid-guid");
        yield return NewValidRequest(x => x.CandidateId = string.Empty);
        yield return NewValidRequest(x => x.State = ProportionalElectionCandidateEndResultState.Unspecified);
    }
}
