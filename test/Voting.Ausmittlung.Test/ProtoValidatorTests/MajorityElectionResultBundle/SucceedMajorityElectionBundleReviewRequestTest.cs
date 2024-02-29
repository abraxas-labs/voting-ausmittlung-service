// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class SucceedMajorityElectionBundleReviewRequestTest : ProtoValidatorBaseTest<SucceedMajorityElectionBundleReviewRequest>
{
    protected override IEnumerable<SucceedMajorityElectionBundleReviewRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<SucceedMajorityElectionBundleReviewRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
    }

    private SucceedMajorityElectionBundleReviewRequest NewValidRequest(Action<SucceedMajorityElectionBundleReviewRequest>? action = null)
    {
        var request = new SucceedMajorityElectionBundleReviewRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
