﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class DefineProportionalElectionResultEntryRequestTest : ProtoValidatorBaseTest<DefineProportionalElectionResultEntryRequest>
{
    protected override IEnumerable<DefineProportionalElectionResultEntryRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DefineProportionalElectionResultEntryRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.ResultEntryParams = null);
    }

    private DefineProportionalElectionResultEntryRequest NewValidRequest(Action<DefineProportionalElectionResultEntryRequest>? action = null)
    {
        var request = new DefineProportionalElectionResultEntryRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            ResultEntryParams = DefineProportionalElectionResultEntryParamsRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
