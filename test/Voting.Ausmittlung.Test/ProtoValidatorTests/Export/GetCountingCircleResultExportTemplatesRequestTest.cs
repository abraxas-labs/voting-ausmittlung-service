// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class GetCountingCircleResultExportTemplatesRequestTest : ProtoValidatorBaseTest<GetCountingCircleResultExportTemplatesRequest>
{
    protected override IEnumerable<GetCountingCircleResultExportTemplatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetCountingCircleResultExportTemplatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessId = "invalid-guid");
        yield return NewValidRequest(x => x.PoliticalBusinessId = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessType = PoliticalBusinessType.Unspecified);
        yield return NewValidRequest(x => x.PoliticalBusinessType = (PoliticalBusinessType)7);
    }

    private GetCountingCircleResultExportTemplatesRequest NewValidRequest(Action<GetCountingCircleResultExportTemplatesRequest>? action = null)
    {
        var request = new GetCountingCircleResultExportTemplatesRequest
        {
            CountingCircleId = "1e9201d5-b5e5-40a5-b044-aa63e2222934",
            PoliticalBusinessId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            PoliticalBusinessType = PoliticalBusinessType.Vote,
        };

        action?.Invoke(request);
        return request;
    }
}
