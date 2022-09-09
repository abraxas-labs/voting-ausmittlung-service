// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class MajorityElectionResultBundleCorrectionFinishedRequestTest : ProtoValidatorBaseTest<MajorityElectionResultBundleCorrectionFinishedRequest>
{
    protected override IEnumerable<MajorityElectionResultBundleCorrectionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<MajorityElectionResultBundleCorrectionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
    }

    private MajorityElectionResultBundleCorrectionFinishedRequest NewValidRequest(Action<MajorityElectionResultBundleCorrectionFinishedRequest>? action = null)
    {
        var request = new MajorityElectionResultBundleCorrectionFinishedRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
