// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Iam.SecondFactor.Testing.Mocks;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultCorrectionFinishedRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultCorrectionFinishedRequest>
{
    protected override IEnumerable<ProportionalElectionResultCorrectionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Comment = string.Empty);
        yield return NewValidRequest(x => x.Comment = new string('A', 500));
        yield return NewValidRequest(x => x.SecondFactorAuthorization = null);
    }

    protected override IEnumerable<ProportionalElectionResultCorrectionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = string.Empty });
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = "invalid-guid" });
        yield return NewValidRequest(x => x.Comment = "Wahlzettt\bel 1 falsch");
        yield return NewValidRequest(x => x.Comment = new string('A', 501));
    }

    private ProportionalElectionResultCorrectionFinishedRequest NewValidRequest(Action<ProportionalElectionResultCorrectionFinishedRequest>? action = null)
    {
        var request = new ProportionalElectionResultCorrectionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Comment = "Wahlzettel 3 falsch\nWahlzettel 5 falsch",
            SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = SecondFactorTransactionServiceMock.VerifiedTransactionIdString },
        };

        action?.Invoke(request);
        return request;
    }
}
