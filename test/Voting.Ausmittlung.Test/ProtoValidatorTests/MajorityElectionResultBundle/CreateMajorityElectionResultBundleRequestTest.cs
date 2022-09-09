// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class CreateMajorityElectionResultBundleRequestTest : ProtoValidatorBaseTest<CreateMajorityElectionResultBundleRequest>
{
    protected override IEnumerable<CreateMajorityElectionResultBundleRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BundleNumber = 1);
        yield return NewValidRequest(x => x.BundleNumber = null);
        yield return NewValidRequest(x => x.BundleNumber = 1000000);
    }

    protected override IEnumerable<CreateMajorityElectionResultBundleRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.BundleNumber = 0);
        yield return NewValidRequest(x => x.BundleNumber = 1000001);
    }

    private CreateMajorityElectionResultBundleRequest NewValidRequest(Action<CreateMajorityElectionResultBundleRequest>? action = null)
    {
        var request = new CreateMajorityElectionResultBundleRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            BundleNumber = 5,
        };

        action?.Invoke(request);
        return request;
    }
}
