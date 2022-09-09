// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class VoteResultFlagForCorrectionRequestTest : ProtoValidatorBaseTest<VoteResultFlagForCorrectionRequest>
{
    protected override IEnumerable<VoteResultFlagForCorrectionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Comment = string.Empty);
    }

    protected override IEnumerable<VoteResultFlagForCorrectionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
        yield return NewValidRequest(x => x.Comment = "Wahlzettt\bel 1 falsch");
    }

    private VoteResultFlagForCorrectionRequest NewValidRequest(Action<VoteResultFlagForCorrectionRequest>? action = null)
    {
        var request = new VoteResultFlagForCorrectionRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Comment = "Wahlzettel 3 falsch\nWahlzettel 5 falsch",
        };

        action?.Invoke(request);
        return request;
    }
}
