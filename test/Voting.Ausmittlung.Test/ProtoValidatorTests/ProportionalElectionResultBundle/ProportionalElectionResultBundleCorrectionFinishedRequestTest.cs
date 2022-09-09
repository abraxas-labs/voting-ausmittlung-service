// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResultBundle;

public class ProportionalElectionResultBundleCorrectionFinishedRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultBundleCorrectionFinishedRequest>
{
    protected override IEnumerable<ProportionalElectionResultBundleCorrectionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ProportionalElectionResultBundleCorrectionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
    }

    private ProportionalElectionResultBundleCorrectionFinishedRequest NewValidRequest(Action<ProportionalElectionResultBundleCorrectionFinishedRequest>? action = null)
    {
        var request = new ProportionalElectionResultBundleCorrectionFinishedRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
