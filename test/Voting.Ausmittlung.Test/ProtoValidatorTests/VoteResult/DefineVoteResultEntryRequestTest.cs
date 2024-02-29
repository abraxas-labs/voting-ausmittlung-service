// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class DefineVoteResultEntryRequestTest : ProtoValidatorBaseTest<DefineVoteResultEntryRequest>
{
    protected override IEnumerable<DefineVoteResultEntryRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ResultEntryParams = null);
    }

    protected override IEnumerable<DefineVoteResultEntryRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultId = "invalid-guid");
        yield return NewValidRequest(x => x.VoteResultId = string.Empty);
        yield return NewValidRequest(x => x.ResultEntry = VoteResultEntry.Unspecified);
        yield return NewValidRequest(x => x.ResultEntry = (VoteResultEntry)(-1));
    }

    private DefineVoteResultEntryRequest NewValidRequest(Action<DefineVoteResultEntryRequest>? action = null)
    {
        var request = new DefineVoteResultEntryRequest
        {
            VoteResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            ResultEntry = VoteResultEntry.Detailed,
            ResultEntryParams = DefineVoteResultEntryParamsRequestTest.NewValidRequest(),
        };

        action?.Invoke(request);
        return request;
    }
}
