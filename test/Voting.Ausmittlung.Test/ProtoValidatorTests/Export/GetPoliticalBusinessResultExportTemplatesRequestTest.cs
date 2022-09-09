// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class GetPoliticalBusinessResultExportTemplatesRequestTest : ProtoValidatorBaseTest<GetPoliticalBusinessResultExportTemplatesRequest>
{
    protected override IEnumerable<GetPoliticalBusinessResultExportTemplatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetPoliticalBusinessResultExportTemplatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.PoliticalBusinessId = "invalid-guid");
        yield return NewValidRequest(x => x.PoliticalBusinessId = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessType = PoliticalBusinessType.Unspecified);
        yield return NewValidRequest(x => x.PoliticalBusinessType = (PoliticalBusinessType)7);
    }

    private GetPoliticalBusinessResultExportTemplatesRequest NewValidRequest(Action<GetPoliticalBusinessResultExportTemplatesRequest>? action = null)
    {
        var request = new GetPoliticalBusinessResultExportTemplatesRequest
        {
            PoliticalBusinessId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            PoliticalBusinessType = PoliticalBusinessType.Vote,
        };

        action?.Invoke(request);
        return request;
    }
}
