// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class ValidateEnterMajorityElectionCandidateResultsRequestTest : ProtoValidatorBaseTest<ValidateEnterMajorityElectionCandidateResultsRequest>
{
    protected override IEnumerable<ValidateEnterMajorityElectionCandidateResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ValidateEnterMajorityElectionCandidateResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Request = null);
    }

    private ValidateEnterMajorityElectionCandidateResultsRequest NewValidRequest(Action<ValidateEnterMajorityElectionCandidateResultsRequest>? action = null)
    {
        var request = new ValidateEnterMajorityElectionCandidateResultsRequest
        {
            Request = EnterMajorityElectionCandidateResultsRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
