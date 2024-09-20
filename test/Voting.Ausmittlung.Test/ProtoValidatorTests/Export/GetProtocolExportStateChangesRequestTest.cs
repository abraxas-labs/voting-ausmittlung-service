// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class GetProtocolExportStateChangesRequestTest : ProtoValidatorBaseTest<GetProtocolExportStateChangesRequest>
{
    protected override IEnumerable<GetProtocolExportStateChangesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
    }

    protected override IEnumerable<GetProtocolExportStateChangesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
    }

    private GetProtocolExportStateChangesRequest NewValidRequest(Action<GetProtocolExportStateChangesRequest>? action = null)
    {
        var request = new GetProtocolExportStateChangesRequest
        {
            ContestId = "f900168c-6381-410a-8af2-4e582c1cbc97",
            CountingCircleId = "7d338f28-9ad8-454a-ae2e-0eae907baec3",
        };

        action?.Invoke(request);
        return request;
    }
}
