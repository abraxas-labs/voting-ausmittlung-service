// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.Export;

public class UpdateResultExportConfigurationRequestTest : ProtoValidatorBaseTest<UpdateResultExportConfigurationRequest>
{
    protected override IEnumerable<UpdateResultExportConfigurationRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.IntervalMinutes = 2);
        yield return NewValidRequest(x => x.IntervalMinutes = 240);
    }

    protected override IEnumerable<UpdateResultExportConfigurationRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.IntervalMinutes = 1);
        yield return NewValidRequest(x => x.IntervalMinutes = 241);
        yield return NewValidRequest(x => x.PoliticalBusinessIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.PoliticalBusinessIds.Add(string.Empty));
    }

    private UpdateResultExportConfigurationRequest NewValidRequest(Action<UpdateResultExportConfigurationRequest>? action = null)
    {
        var request = new UpdateResultExportConfigurationRequest
        {
            ContestId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            ExportConfigurationId = "ea2e8ab7-8916-45e5-8a0b-632d28a00c6a",
            PoliticalBusinessIds =
            {
                "45c86141-3742-4914-8877-20c65777445f",
            },
            IntervalMinutes = 30,
        };

        action?.Invoke(request);
        return request;
    }
}
