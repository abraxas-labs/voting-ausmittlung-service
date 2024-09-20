// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.ProtoValidatorTests.CountOfVoters;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class EnterMajorityElectionCountOfVotersRequestTest : ProtoValidatorBaseTest<EnterMajorityElectionCountOfVotersRequest>
{
    public static EnterMajorityElectionCountOfVotersRequest NewValidRequest(Action<EnterMajorityElectionCountOfVotersRequest>? action = null)
    {
        var request = new EnterMajorityElectionCountOfVotersRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            CountOfVoters = EnterPoliticalBusinessCountOfVotersRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterMajorityElectionCountOfVotersRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<EnterMajorityElectionCountOfVotersRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.CountOfVoters = null);
    }
}
