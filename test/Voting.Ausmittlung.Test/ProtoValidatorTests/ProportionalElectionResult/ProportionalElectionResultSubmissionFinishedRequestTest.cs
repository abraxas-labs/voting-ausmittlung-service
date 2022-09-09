// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultSubmissionFinishedRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultSubmissionFinishedRequest>
{
    protected override IEnumerable<ProportionalElectionResultSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ProportionalElectionResultSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    private ProportionalElectionResultSubmissionFinishedRequest NewValidRequest(Action<ProportionalElectionResultSubmissionFinishedRequest>? action = null)
    {
        var request = new ProportionalElectionResultSubmissionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
