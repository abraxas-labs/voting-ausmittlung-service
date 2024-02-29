// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class MajorityElectionResultCorrectionFinishedRequestTest : ProtoValidatorBaseTest<MajorityElectionResultCorrectionFinishedRequest>
{
    protected override IEnumerable<MajorityElectionResultCorrectionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Comment = string.Empty);
        yield return NewValidRequest(x => x.Comment = new string('A', 500));
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    protected override IEnumerable<MajorityElectionResultCorrectionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = "invalid-guid");
        yield return NewValidRequest(x => x.Comment = "Wahlzettt\bel 1 falsch");
        yield return NewValidRequest(x => x.Comment = new string('A', 501));
    }

    private MajorityElectionResultCorrectionFinishedRequest NewValidRequest(Action<MajorityElectionResultCorrectionFinishedRequest>? action = null)
    {
        var request = new MajorityElectionResultCorrectionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Comment = "Wahlzettel 3 falsch\nWahlzettel 5 falsch",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
