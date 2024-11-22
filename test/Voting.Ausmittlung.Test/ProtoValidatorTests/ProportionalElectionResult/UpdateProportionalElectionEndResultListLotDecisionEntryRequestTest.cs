// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class UpdateProportionalElectionEndResultListLotDecisionEntryRequestTest : ProtoValidatorBaseTest<UpdateProportionalElectionEndResultListLotDecisionEntryRequest>
{
    public static UpdateProportionalElectionEndResultListLotDecisionEntryRequest NewValidRequest(Action<UpdateProportionalElectionEndResultListLotDecisionEntryRequest>? action = null)
    {
        var request = new UpdateProportionalElectionEndResultListLotDecisionEntryRequest
        {
            ListId = "7b419d17-4f87-4fab-b856-8d8fd33e9aec",
            ListUnionId = "7b419d17-4f87-4fab-b856-8d8fd33e9aec",
            Winning = true,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<UpdateProportionalElectionEndResultListLotDecisionEntryRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ListId = string.Empty);
        yield return NewValidRequest(x => x.ListUnionId = string.Empty);
    }

    protected override IEnumerable<UpdateProportionalElectionEndResultListLotDecisionEntryRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ListId = "invalid-guid");
        yield return NewValidRequest(x => x.ListUnionId = "invalid-guid");
    }
}
