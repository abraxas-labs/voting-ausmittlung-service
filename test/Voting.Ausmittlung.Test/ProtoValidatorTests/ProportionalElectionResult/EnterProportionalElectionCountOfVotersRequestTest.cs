// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.ProtoValidatorTests.CountOfVoters;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class EnterProportionalElectionCountOfVotersRequestTest : ProtoValidatorBaseTest<EnterProportionalElectionCountOfVotersRequest>
{
    public static EnterProportionalElectionCountOfVotersRequest NewValidRequest(Action<EnterProportionalElectionCountOfVotersRequest>? action = null)
    {
        var request = new EnterProportionalElectionCountOfVotersRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            CountOfVoters = EnterPoliticalBusinessCountOfVotersRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterProportionalElectionCountOfVotersRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<EnterProportionalElectionCountOfVotersRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.CountOfVoters = null);
    }
}
