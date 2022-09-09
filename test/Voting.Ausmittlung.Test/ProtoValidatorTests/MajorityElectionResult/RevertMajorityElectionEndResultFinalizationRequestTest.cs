// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class RevertMajorityElectionEndResultFinalizationRequestTest : ProtoValidatorBaseTest<RevertMajorityElectionEndResultFinalizationRequest>
{
    protected override IEnumerable<RevertMajorityElectionEndResultFinalizationRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<RevertMajorityElectionEndResultFinalizationRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
    }

    private RevertMajorityElectionEndResultFinalizationRequest NewValidRequest(Action<RevertMajorityElectionEndResultFinalizationRequest>? action = null)
    {
        var request = new RevertMajorityElectionEndResultFinalizationRequest
        {
            MajorityElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
