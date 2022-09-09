// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class PrepareFinalizeMajorityElectionEndResultRequestTest : ProtoValidatorBaseTest<PrepareFinalizeMajorityElectionEndResultRequest>
{
    protected override IEnumerable<PrepareFinalizeMajorityElectionEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<PrepareFinalizeMajorityElectionEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
    }

    private PrepareFinalizeMajorityElectionEndResultRequest NewValidRequest(Action<PrepareFinalizeMajorityElectionEndResultRequest>? action = null)
    {
        var request = new PrepareFinalizeMajorityElectionEndResultRequest
        {
            MajorityElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
