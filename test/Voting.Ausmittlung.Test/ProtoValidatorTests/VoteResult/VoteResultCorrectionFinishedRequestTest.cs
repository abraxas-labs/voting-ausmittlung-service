// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class VoteResultCorrectionFinishedRequestTest : ProtoValidatorBaseTest<VoteResultCorrectionFinishedRequest>
{
    protected override IEnumerable<VoteResultCorrectionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Comment = string.Empty);
        yield return NewValidRequest(x => x.Comment = new string('A', 500));
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    protected override IEnumerable<VoteResultCorrectionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = "invalid-guid");
        yield return NewValidRequest(x => x.Comment = "Wahlzettt\bel 1 falsch");
        yield return NewValidRequest(x => x.Comment = new string('A', 501));
    }

    private VoteResultCorrectionFinishedRequest NewValidRequest(Action<VoteResultCorrectionFinishedRequest>? action = null)
    {
        var request = new VoteResultCorrectionFinishedRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Comment = "Wahlzettel 3 falsch\nWahlzettel 5 falsch",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };

        action?.Invoke(request);
        return request;
    }
}
