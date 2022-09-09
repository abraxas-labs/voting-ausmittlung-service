// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class FinalizeMajorityElectionEndResultRequestTest : ProtoValidatorBaseTest<FinalizeMajorityElectionEndResultRequest>
{
    protected override IEnumerable<FinalizeMajorityElectionEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<FinalizeMajorityElectionEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    private FinalizeMajorityElectionEndResultRequest NewValidRequest(Action<FinalizeMajorityElectionEndResultRequest>? action = null)
    {
        var request = new FinalizeMajorityElectionEndResultRequest
        {
            MajorityElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
