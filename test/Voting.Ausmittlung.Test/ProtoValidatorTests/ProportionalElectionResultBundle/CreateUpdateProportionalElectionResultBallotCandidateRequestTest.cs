// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResultBundle;

public class CreateUpdateProportionalElectionResultBallotCandidateRequestTest : ProtoValidatorBaseTest<CreateUpdateProportionalElectionResultBallotCandidateRequest>
{
    public static CreateUpdateProportionalElectionResultBallotCandidateRequest NewValidRequest(Action<CreateUpdateProportionalElectionResultBallotCandidateRequest>? action = null)
    {
        var request = new CreateUpdateProportionalElectionResultBallotCandidateRequest
        {
            CandidateId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            OnList = true,
            Position = 2,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<CreateUpdateProportionalElectionResultBallotCandidateRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Position = 1);
        yield return NewValidRequest(x => x.Position = 50);
        yield return NewValidRequest(x => x.OnList = false);
    }

    protected override IEnumerable<CreateUpdateProportionalElectionResultBallotCandidateRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CandidateId = "invalid-guid");
        yield return NewValidRequest(x => x.CandidateId = string.Empty);
        yield return NewValidRequest(x => x.Position = 0);
        yield return NewValidRequest(x => x.Position = 51);
    }
}
