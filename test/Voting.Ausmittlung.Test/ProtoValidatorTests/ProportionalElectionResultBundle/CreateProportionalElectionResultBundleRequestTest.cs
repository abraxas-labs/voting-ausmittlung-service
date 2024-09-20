// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResultBundle;

public class CreateProportionalElectionResultBundleRequestTest : ProtoValidatorBaseTest<CreateProportionalElectionResultBundleRequest>
{
    protected override IEnumerable<CreateProportionalElectionResultBundleRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BundleNumber = 1);
        yield return NewValidRequest(x => x.BundleNumber = null);
        yield return NewValidRequest(x => x.BundleNumber = 1000000);
    }

    protected override IEnumerable<CreateProportionalElectionResultBundleRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.BundleNumber = 0);
        yield return NewValidRequest(x => x.BundleNumber = 1000001);
    }

    private CreateProportionalElectionResultBundleRequest NewValidRequest(Action<CreateProportionalElectionResultBundleRequest>? action = null)
    {
        var request = new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            ListId = "c4dee637-6b69-4338-9cf8-5f32f3b9cabb",
            BundleNumber = 5,
        };

        action?.Invoke(request);
        return request;
    }
}
