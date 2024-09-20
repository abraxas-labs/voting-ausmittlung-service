// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajoritylElectionResult;

public class GetMajorityElectionEndResultAvailableLotDecisionsRequestTest : ProtoValidatorBaseTest<GetMajorityElectionEndResultAvailableLotDecisionsRequest>
{
    protected override IEnumerable<GetMajorityElectionEndResultAvailableLotDecisionsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetMajorityElectionEndResultAvailableLotDecisionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
    }

    private GetMajorityElectionEndResultAvailableLotDecisionsRequest NewValidRequest(Action<GetMajorityElectionEndResultAvailableLotDecisionsRequest>? action = null)
    {
        var request = new GetMajorityElectionEndResultAvailableLotDecisionsRequest
        {
            MajorityElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
