// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class GetBundleReviewExportStateChangesRequestTest : ProtoValidatorBaseTest<GetBundleReviewExportStateChangesRequest>
{
    protected override IEnumerable<GetBundleReviewExportStateChangesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetBundleReviewExportStateChangesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.PoliticalBusinessResultId = "invalid-guid");
        yield return NewValidRequest(x => x.PoliticalBusinessResultId = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessType = PoliticalBusinessType.Unspecified);
        yield return NewValidRequest(x => x.PoliticalBusinessType = (PoliticalBusinessType)6);
    }

    private GetBundleReviewExportStateChangesRequest NewValidRequest(Action<GetBundleReviewExportStateChangesRequest>? action = null)
    {
        var request = new GetBundleReviewExportStateChangesRequest
        {
            PoliticalBusinessResultId = "23a0eabe-67bc-4d32-97ae-025b8eb93a5a",
            PoliticalBusinessType = PoliticalBusinessType.Vote,
        };

        action?.Invoke(request);
        return request;
    }
}
