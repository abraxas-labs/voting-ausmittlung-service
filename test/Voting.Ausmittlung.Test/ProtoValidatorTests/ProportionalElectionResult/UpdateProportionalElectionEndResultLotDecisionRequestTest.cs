// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class UpdateProportionalElectionEndResultLotDecisionRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionEndResultLotDecisionRequest>
{
    public static UpdateProportionalElectionEndResultLotDecisionRequest NewValidRequest(Action<UpdateProportionalElectionEndResultLotDecisionRequest>? action = null)
    {
        var request = new UpdateProportionalElectionEndResultLotDecisionRequest
        {
            CandidateId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Rank = 2,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<UpdateProportionalElectionEndResultLotDecisionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Rank = 1);
        yield return NewValidRequest(x => x.Rank = 50);
    }

    protected override IEnumerable<UpdateProportionalElectionEndResultLotDecisionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CandidateId = "invalid-guid");
        yield return NewValidRequest(x => x.CandidateId = string.Empty);
        yield return NewValidRequest(x => x.Rank = 0);
        yield return NewValidRequest(x => x.Rank = 51);
    }
}
