// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class VoteResultAuditedTentativelyRequestTest : ProtoValidatorBaseTest<VoteResultAuditedTentativelyRequest>
{
    protected override IEnumerable<VoteResultAuditedTentativelyRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.VoteResultIds.Clear());
    }

    protected override IEnumerable<VoteResultAuditedTentativelyRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.VoteResultIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.VoteResultIds.Add(string.Empty));
    }

    private VoteResultAuditedTentativelyRequest NewValidRequest(Action<VoteResultAuditedTentativelyRequest>? action = null)
    {
        var request = new VoteResultAuditedTentativelyRequest
        {
            VoteResultIds =
            {
                "f67b688a-0566-4e3c-bd73-6063834fedaf",
                "87a982e0-b8df-4318-b787-28a0c01693a4",
            },
        };

        action?.Invoke(request);
        return request;
    }
}
