// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class StartProtocolExportsRequestTest : ProtoValidatorBaseTest<StartProtocolExportsRequest>
{
    protected override IEnumerable<StartProtocolExportsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CountingCircleId = "8f7e230e-a231-45a9-b994-32d4b8b64f0b");
        yield return NewValidRequest(x => x.ExportTemplateIds.Add("b412bc72-623a-4625-90fa-7c02cb4d6ff8"));
    }

    protected override IEnumerable<StartProtocolExportsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.ExportTemplateIds[0] = "invalid-guid");
        yield return NewValidRequest(x => x.ExportTemplateIds[0] = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
    }

    private StartProtocolExportsRequest NewValidRequest(Action<StartProtocolExportsRequest>? action = null)
    {
        var request = new StartProtocolExportsRequest
        {
            ContestId = "f900168c-6381-410a-8af2-4e582c1cbc97",
            ExportTemplateIds = { "7d338f28-9ad8-454a-ae2e-0eae907baec3" },
        };

        action?.Invoke(request);
        return request;
    }
}
