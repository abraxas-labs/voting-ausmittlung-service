﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class UpdateMajorityElectionEndResultLotDecisionRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionEndResultLotDecisionRequest>
{
    public static UpdateMajorityElectionEndResultLotDecisionRequest NewValidRequest(Action<UpdateMajorityElectionEndResultLotDecisionRequest>? action = null)
    {
        var request = new UpdateMajorityElectionEndResultLotDecisionRequest
        {
            CandidateId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            Rank = 2,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<UpdateMajorityElectionEndResultLotDecisionRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.Rank = null);
        yield return NewValidRequest(x => x.Rank = 1);
        yield return NewValidRequest(x => x.Rank = 100);
    }

    protected override IEnumerable<UpdateMajorityElectionEndResultLotDecisionRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CandidateId = "invalid-guid");
        yield return NewValidRequest(x => x.CandidateId = string.Empty);
        yield return NewValidRequest(x => x.Rank = 0);
        yield return NewValidRequest(x => x.Rank = 101);
    }
}
