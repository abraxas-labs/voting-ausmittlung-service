// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class FinalizeProportionalElectionEndResultRequestTest : ProtoValidatorBaseTest<FinalizeProportionalElectionEndResultRequest>
{
    protected override IEnumerable<FinalizeProportionalElectionEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<FinalizeProportionalElectionEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    private FinalizeProportionalElectionEndResultRequest NewValidRequest(Action<FinalizeProportionalElectionEndResultRequest>? action = null)
    {
        var request = new FinalizeProportionalElectionEndResultRequest
        {
            ProportionalElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
