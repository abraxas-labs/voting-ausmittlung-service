// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class UpdateMajorityElectionEndResultLotDecisionsRequestTest : ProtoValidatorBaseTest<UpdateMajorityElectionEndResultLotDecisionsRequest>
{
    protected override IEnumerable<UpdateMajorityElectionEndResultLotDecisionsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.LotDecisions.Clear());
    }

    protected override IEnumerable<UpdateMajorityElectionEndResultLotDecisionsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.MajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.MajorityElectionId = string.Empty);
    }

    private UpdateMajorityElectionEndResultLotDecisionsRequest NewValidRequest(Action<UpdateMajorityElectionEndResultLotDecisionsRequest>? action = null)
    {
        var request = new UpdateMajorityElectionEndResultLotDecisionsRequest
        {
            MajorityElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            LotDecisions =
            {
                UpdateMajorityElectionEndResultLotDecisionRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
