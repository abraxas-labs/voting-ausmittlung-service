// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Iam.SecondFactor.Testing.Mocks;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class MajorityElectionResultSubmissionFinishedRequestTest : ProtoValidatorBaseTest<MajorityElectionResultSubmissionFinishedRequest>
{
    protected override IEnumerable<MajorityElectionResultSubmissionFinishedRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.SecondFactorAuthorization = null);
    }

    protected override IEnumerable<MajorityElectionResultSubmissionFinishedRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = string.Empty });
        yield return NewValidRequest(x => x.SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = "invalid-guid" });
    }

    private MajorityElectionResultSubmissionFinishedRequest NewValidRequest(Action<MajorityElectionResultSubmissionFinishedRequest>? action = null)
    {
        var request = new MajorityElectionResultSubmissionFinishedRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            SecondFactorAuthorization = new SecondFactorAuthorization { SecondFactorTransactionId = SecondFactorTransactionServiceMock.VerifiedTransactionIdString },
        };

        action?.Invoke(request);
        return request;
    }
}
