// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class ListResultExportConfigurationsRequestTest : ProtoValidatorBaseTest<ListResultExportConfigurationsRequest>
{
    protected override IEnumerable<ListResultExportConfigurationsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListResultExportConfigurationsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
    }

    private ListResultExportConfigurationsRequest NewValidRequest(Action<ListResultExportConfigurationsRequest>? action = null)
    {
        var request = new ListResultExportConfigurationsRequest
        {
            ContestId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
