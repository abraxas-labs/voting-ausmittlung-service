// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class VoteResultSubmissionFinishedRequestTest : ProtoValidatorBaseTest<VoteResultSubmissionFinishedRequest>
{
    protected override IEnumerable<VoteResultSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    protected override IEnumerable<VoteResultSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = "invalid-guid");
    }

    private VoteResultSubmissionFinishedRequest NewValidRequest(Action<VoteResultSubmissionFinishedRequest>? action = null)
    {
        var request = new VoteResultSubmissionFinishedRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
