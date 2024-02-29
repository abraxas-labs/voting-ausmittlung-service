// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class EnterProportionalElectionManualListEndResultRequestTest : ProtoValidatorBaseTest<EnterProportionalElectionManualListEndResultRequest>
{
    protected override IEnumerable<EnterProportionalElectionManualListEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CandidateEndResults.Clear());
    }

    protected override IEnumerable<EnterProportionalElectionManualListEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionListId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionListId = string.Empty);
    }

    private EnterProportionalElectionManualListEndResultRequest NewValidRequest(Action<EnterProportionalElectionManualListEndResultRequest>? action = null)
    {
        var request = new EnterProportionalElectionManualListEndResultRequest
        {
            ProportionalElectionListId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            CandidateEndResults =
            {
                EnterProportionalElectionManualCandidateEndResultRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
