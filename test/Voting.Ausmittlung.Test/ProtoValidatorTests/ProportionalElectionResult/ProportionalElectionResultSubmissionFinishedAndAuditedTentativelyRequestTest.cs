// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultSubmissionFinishedAndAuditedTentativelyRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultSubmissionFinishedAndAuditedTentativelyRequest>
{
    protected override IEnumerable<ProportionalElectionResultSubmissionFinishedAndAuditedTentativelyRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ProportionalElectionResultSubmissionFinishedAndAuditedTentativelyRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorTransactionId = "invalid-guid");
    }

    private ProportionalElectionResultSubmissionFinishedAndAuditedTentativelyRequest NewValidRequest(Action<ProportionalElectionResultSubmissionFinishedAndAuditedTentativelyRequest>? action = null)
    {
        var request = new ProportionalElectionResultSubmissionFinishedAndAuditedTentativelyRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };

        action?.Invoke(request);
        return request;
    }
}
