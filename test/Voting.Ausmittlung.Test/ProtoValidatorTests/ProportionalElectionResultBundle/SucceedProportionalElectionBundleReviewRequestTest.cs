// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResultBundle;

public class SucceedProportionalElectionBundleReviewRequestTest : ProtoValidatorBaseTest<SucceedProportionalElectionBundleReviewRequest>
{
    protected override IEnumerable<SucceedProportionalElectionBundleReviewRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BundleIds.Clear());
    }

    protected override IEnumerable<SucceedProportionalElectionBundleReviewRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.BundleIds.Add(string.Empty));
    }

    private SucceedProportionalElectionBundleReviewRequest NewValidRequest(Action<SucceedProportionalElectionBundleReviewRequest>? action = null)
    {
        var request = new SucceedProportionalElectionBundleReviewRequest
        {
            BundleIds =
            {
                "f67b688a-0566-4e3c-bd73-6063834fedaf",
                "87a982e0-b8df-4318-b787-28a0c01693a4",
            },
        };

        action?.Invoke(request);
        return request;
    }
}
