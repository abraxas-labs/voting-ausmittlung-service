// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class MajorityElectionResultSubmissionFinishedRequestTest : ProtoValidatorBaseTest<MajorityElectionResultSubmissionFinishedRequest>
{
    protected override IEnumerable<MajorityElectionResultSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
    }

    protected override IEnumerable<MajorityElectionResultSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = "invalid-guid");
    }

    private MajorityElectionResultSubmissionFinishedRequest NewValidRequest(Action<MajorityElectionResultSubmissionFinishedRequest>? action = null)
    {
        var request = new MajorityElectionResultSubmissionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
