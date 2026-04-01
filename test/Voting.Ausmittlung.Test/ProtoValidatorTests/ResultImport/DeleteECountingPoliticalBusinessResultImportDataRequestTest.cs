// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ResultImport;

public class DeleteECountingPoliticalBusinessResultImportDataRequestTest : ProtoValidatorBaseTest<DeleteECountingResultPoliticalBusinessImportDataRequest>
{
    protected override IEnumerable<DeleteECountingResultPoliticalBusinessImportDataRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DeleteECountingResultPoliticalBusinessImportDataRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessId = "invalid-guid");
        yield return NewValidRequest(x => x.PoliticalBusinessId = string.Empty);
    }

    private DeleteECountingResultPoliticalBusinessImportDataRequest NewValidRequest(Action<DeleteECountingResultPoliticalBusinessImportDataRequest>? action = null)
    {
        var request = new DeleteECountingResultPoliticalBusinessImportDataRequest
        {
            ContestId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
            CountingCircleId = "199ecbe6-1ee4-4916-8dbf-76e9fb682ca4",
            PoliticalBusinessId = "701afd41-7f56-4971-8dbe-a0d80c629143",
        };

        action?.Invoke(request);
        return request;
    }
}
