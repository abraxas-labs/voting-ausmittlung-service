﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultFlagForCorrectionRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultFlagForCorrectionRequest>
{
    protected override IEnumerable<ProportionalElectionResultFlagForCorrectionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Comment = string.Empty);
        yield return NewValidRequest(x => x.Comment = new string('A', 500));
    }

    protected override IEnumerable<ProportionalElectionResultFlagForCorrectionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.Comment = "Wahlzettt\bel 1 falsch");
        yield return NewValidRequest(x => x.Comment = new string('A', 501));
    }

    private ProportionalElectionResultFlagForCorrectionRequest NewValidRequest(Action<ProportionalElectionResultFlagForCorrectionRequest>? action = null)
    {
        var request = new ProportionalElectionResultFlagForCorrectionRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Comment = "Wahlzettel 3 falsch\nWahlzettel 5 falsch",
        };

        action?.Invoke(request);
        return request;
    }
}
