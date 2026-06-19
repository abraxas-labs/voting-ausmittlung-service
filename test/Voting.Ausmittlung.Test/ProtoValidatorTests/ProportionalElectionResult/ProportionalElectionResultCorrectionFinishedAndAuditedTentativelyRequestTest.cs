// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Iam.SecondFactor.Testing.Mocks;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest>
{
    protected override IEnumerable<ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = string.Empty });
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = "invalid-guid" });
    }

    private ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest NewValidRequest(Action<ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest>? action = null)
    {
        var request = new ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = SecondFactorTransactionServiceMock.VerifiedTransactionIdString },
        };

        action?.Invoke(request);
        return request;
    }
}
