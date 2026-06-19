// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Iam.SecondFactor.Testing.Mocks;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class VoteResultSubmissionFinishedAndAuditedTentativelyRequestTest : ProtoValidatorBaseTest<VoteResultSubmissionFinishedAndAuditedTentativelyRequest>
{
    protected override IEnumerable<VoteResultSubmissionFinishedAndAuditedTentativelyRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<VoteResultSubmissionFinishedAndAuditedTentativelyRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = string.Empty });
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = "invalid-guid" });
    }

    private VoteResultSubmissionFinishedAndAuditedTentativelyRequest NewValidRequest(Action<VoteResultSubmissionFinishedAndAuditedTentativelyRequest>? action = null)
    {
        var request = new VoteResultSubmissionFinishedAndAuditedTentativelyRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = SecondFactorTransactionServiceMock.VerifiedTransactionIdString },
        };

        action?.Invoke(request);
        return request;
    }
}
