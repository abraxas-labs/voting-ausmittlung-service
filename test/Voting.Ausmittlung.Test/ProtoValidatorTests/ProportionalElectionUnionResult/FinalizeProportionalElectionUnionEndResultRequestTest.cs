// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionUnionResult;

public class FinalizeProportionalElectionUnionEndResultRequestTest : ProtoValidatorBaseTest<FinalizeProportionalElectionUnionEndResultRequest>
{
    protected override IEnumerable<FinalizeProportionalElectionUnionEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<FinalizeProportionalElectionUnionEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = "invalid-guid");
        yield return NewValidRequest(x => x.ProportionalElectionUnionId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    private FinalizeProportionalElectionUnionEndResultRequest NewValidRequest(Action<FinalizeProportionalElectionUnionEndResultRequest>? action = null)
    {
        var request = new FinalizeProportionalElectionUnionEndResultRequest
        {
            ProportionalElectionUnionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
