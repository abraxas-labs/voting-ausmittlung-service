// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class EnterMajorityElectionCandidateResultRequestTest : ProtoValidatorBaseTest<EnterMajorityElectionCandidateResultRequest>
{
    public static EnterMajorityElectionCandidateResultRequest NewValidRequest(Action<EnterMajorityElectionCandidateResultRequest>? action = null)
    {
        var request = new EnterMajorityElectionCandidateResultRequest
        {
            CandidateId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            VoteCount = 2,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterMajorityElectionCandidateResultRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.VoteCount = 0);
        yield return NewValidRequest(x => x.VoteCount = null);
        yield return NewValidRequest(x => x.VoteCount = 1000000);
    }

    protected override IEnumerable<EnterMajorityElectionCandidateResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CandidateId = "invalid-guid");
        yield return NewValidRequest(x => x.CandidateId = string.Empty);
        yield return NewValidRequest(x => x.VoteCount = -1);
        yield return NewValidRequest(x => x.VoteCount = 1000001);
    }
}
