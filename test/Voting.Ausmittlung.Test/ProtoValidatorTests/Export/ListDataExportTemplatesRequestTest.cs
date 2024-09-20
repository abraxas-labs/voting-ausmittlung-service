// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class ListDataExportTemplatesRequestTest : ProtoValidatorBaseTest<ListDataExportTemplatesRequest>
{
    protected override IEnumerable<ListDataExportTemplatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CountingCircleId = "b72e28d3-e502-4706-a43e-2d588adb8bb9");
    }

    protected override IEnumerable<ListDataExportTemplatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
    }

    private ListDataExportTemplatesRequest NewValidRequest(Action<ListDataExportTemplatesRequest>? action = null)
    {
        var request = new ListDataExportTemplatesRequest
        {
            ContestId = "f900168c-6381-410a-8af2-4e582c1cbc97",
        };

        action?.Invoke(request);
        return request;
    }
}
