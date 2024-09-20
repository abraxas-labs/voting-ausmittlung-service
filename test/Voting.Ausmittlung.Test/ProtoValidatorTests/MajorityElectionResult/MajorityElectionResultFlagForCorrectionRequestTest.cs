// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class MajorityElectionResultFlagForCorrectionRequestTest : ProtoValidatorBaseTest<MajorityElectionResultFlagForCorrectionRequest>
{
    protected override IEnumerable<MajorityElectionResultFlagForCorrectionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Comment = string.Empty);
        yield return NewValidRequest(x => x.Comment = new string('A', 500));
    }

    protected override IEnumerable<MajorityElectionResultFlagForCorrectionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.Comment = "Wahlzettt\bel 1 falsch");
        yield return NewValidRequest(x => x.Comment = new string('A', 501));
    }

    private MajorityElectionResultFlagForCorrectionRequest NewValidRequest(Action<MajorityElectionResultFlagForCorrectionRequest>? action = null)
    {
        var request = new MajorityElectionResultFlagForCorrectionRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Comment = "Wahlzettel 3 falsch\nWahlzettel 5 falsch",
        };

        action?.Invoke(request);
        return request;
    }
}
