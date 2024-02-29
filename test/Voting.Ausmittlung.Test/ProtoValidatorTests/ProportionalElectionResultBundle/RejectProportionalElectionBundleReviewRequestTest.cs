// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResultBundle;

public class RejectProportionalElectionBundleReviewRequestTest : ProtoValidatorBaseTest<RejectProportionalElectionBundleReviewRequest>
{
    protected override IEnumerable<RejectProportionalElectionBundleReviewRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<RejectProportionalElectionBundleReviewRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
    }

    private RejectProportionalElectionBundleReviewRequest NewValidRequest(Action<RejectProportionalElectionBundleReviewRequest>? action = null)
    {
        var request = new RejectProportionalElectionBundleReviewRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
