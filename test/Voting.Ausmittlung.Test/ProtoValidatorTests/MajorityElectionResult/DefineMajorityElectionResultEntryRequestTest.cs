// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class DefineMajorityElectionResultEntryRequestTest : ProtoValidatorBaseTest<DefineMajorityElectionResultEntryRequest>
{
    protected override IEnumerable<DefineMajorityElectionResultEntryRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ResultEntryParams = null);
    }

    protected override IEnumerable<DefineMajorityElectionResultEntryRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.ResultEntry = MajorityElectionResultEntry.Unspecified);
        yield return NewValidRequest(x => x.ResultEntry = (MajorityElectionResultEntry)22);
    }

    private DefineMajorityElectionResultEntryRequest NewValidRequest(Action<DefineMajorityElectionResultEntryRequest>? action = null)
    {
        var request = new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            ResultEntry = MajorityElectionResultEntry.FinalResults,
            ResultEntryParams = DefineMajorityElectionResultEntryParamsRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
