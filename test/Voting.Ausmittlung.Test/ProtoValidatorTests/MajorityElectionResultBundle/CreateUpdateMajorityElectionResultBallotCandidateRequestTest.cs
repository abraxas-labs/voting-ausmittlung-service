// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResultBundle;

public class CreateUpdateMajorityElectionResultBallotCandidateRequestTest : ProtoValidatorBaseTest<CreateUpdateMajorityElectionResultBallotCandidateRequest>
{
    public static CreateUpdateMajorityElectionResultBallotCandidateRequest NewValidRequest(Action<CreateUpdateMajorityElectionResultBallotCandidateRequest>? action = null)
    {
        var request = new CreateUpdateMajorityElectionResultBallotCandidateRequest
        {
            CandidateId = "e66586f4-ddcd-4b1b-a011-0aec5005e2b1",
            Position = 1,
            OnBallotGroup = true,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<CreateUpdateMajorityElectionResultBallotCandidateRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.OnBallotGroup = false);
    }

    protected override IEnumerable<CreateUpdateMajorityElectionResultBallotCandidateRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CandidateId = "invalid-guid");
        yield return NewValidRequest(x => x.CandidateId = string.Empty);
        yield return NewValidRequest(x => x.Position = -1);
        yield return NewValidRequest(x => x.Position = 101);
    }
}
