// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class DeleteMajorityElectionResultBundleRequestTest : ProtoValidatorBaseTest<DeleteMajorityElectionResultBundleRequest>
{
    protected override IEnumerable<DeleteMajorityElectionResultBundleRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DeleteMajorityElectionResultBundleRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
    }

    private DeleteMajorityElectionResultBundleRequest NewValidRequest(Action<DeleteMajorityElectionResultBundleRequest>? action = null)
    {
        var request = new DeleteMajorityElectionResultBundleRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
