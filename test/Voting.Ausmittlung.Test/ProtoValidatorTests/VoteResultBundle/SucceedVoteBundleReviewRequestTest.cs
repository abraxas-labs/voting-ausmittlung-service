// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResultBundle;

public class SucceedVoteBundleReviewRequestTest : ProtoValidatorBaseTest<SucceedVoteBundleReviewRequest>
{
    protected override IEnumerable<SucceedVoteBundleReviewRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BundleIds.Clear());
    }

    protected override IEnumerable<SucceedVoteBundleReviewRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.BundleIds.Add(string.Empty));
    }

    private SucceedVoteBundleReviewRequest NewValidRequest(Action<SucceedVoteBundleReviewRequest>? action = null)
    {
        var request = new SucceedVoteBundleReviewRequest
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
