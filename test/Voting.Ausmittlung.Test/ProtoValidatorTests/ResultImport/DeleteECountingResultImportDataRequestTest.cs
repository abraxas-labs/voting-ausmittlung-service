// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ResultImport;

public class DeleteECountingResultImportDataRequestTest : ProtoValidatorBaseTest<DeleteECountingResultImportDataRequest>
{
    protected override IEnumerable<DeleteECountingResultImportDataRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DeleteECountingResultImportDataRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
    }

    private DeleteECountingResultImportDataRequest NewValidRequest(Action<DeleteECountingResultImportDataRequest>? action = null)
    {
        var request = new DeleteECountingResultImportDataRequest
        {
            ContestId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
            CountingCircleId = "199ecbe6-1ee4-4916-8dbf-76e9fb682ca4",
        };

        action?.Invoke(request);
        return request;
    }
}
