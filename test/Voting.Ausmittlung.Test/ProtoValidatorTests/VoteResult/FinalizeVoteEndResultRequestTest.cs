// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class FinalizeVoteEndResultRequestTest : ProtoValidatorBaseTest<FinalizeVoteEndResultRequest>
{
    protected override IEnumerable<FinalizeVoteEndResultRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<FinalizeVoteEndResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    private FinalizeVoteEndResultRequest NewValidRequest(Action<FinalizeVoteEndResultRequest>? action = null)
    {
        var request = new FinalizeVoteEndResultRequest
        {
            VoteId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
