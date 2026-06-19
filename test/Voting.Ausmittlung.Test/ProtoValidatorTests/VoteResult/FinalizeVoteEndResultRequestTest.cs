// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Iam.SecondFactor.Testing.Mocks;
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
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = "invalid-guid" });
    }

    private FinalizeVoteEndResultRequest NewValidRequest(Action<FinalizeVoteEndResultRequest>? action = null)
    {
        var request = new FinalizeVoteEndResultRequest
        {
            VoteId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = SecondFactorTransactionServiceMock.VerifiedTransactionIdString },
        };

        action?.Invoke(request);
        return request;
    }
}
