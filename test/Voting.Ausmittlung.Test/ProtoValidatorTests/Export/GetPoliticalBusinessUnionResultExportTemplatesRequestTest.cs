// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class GetPoliticalBusinessUnionResultExportTemplatesRequestTest : ProtoValidatorBaseTest<GetPoliticalBusinessUnionResultExportTemplatesRequest>
{
    protected override IEnumerable<GetPoliticalBusinessUnionResultExportTemplatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<GetPoliticalBusinessUnionResultExportTemplatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.PoliticalBusinessUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.PoliticalBusinessUnionId = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessType = PoliticalBusinessType.Unspecified);
        yield return NewValidRequest(x => x.PoliticalBusinessType = (PoliticalBusinessType)(-1));
    }

    private GetPoliticalBusinessUnionResultExportTemplatesRequest NewValidRequest(Action<GetPoliticalBusinessUnionResultExportTemplatesRequest>? action = null)
    {
        var request = new GetPoliticalBusinessUnionResultExportTemplatesRequest
        {
            PoliticalBusinessUnionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            PoliticalBusinessType = PoliticalBusinessType.MajorityElection,
        };

        action?.Invoke(request);
        return request;
    }
}
