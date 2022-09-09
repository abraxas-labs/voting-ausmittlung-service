// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequestTest : ProtoValidatorBaseTest<GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest>
{
    protected override IEnumerable<GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
    }

    private GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest NewValidRequest(Action<GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest>? action = null)
    {
        var request = new GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest
        {
            CountingCircleId = "1e9201d5-b5e5-40a5-b044-aa63e2222934",
            ContestId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
