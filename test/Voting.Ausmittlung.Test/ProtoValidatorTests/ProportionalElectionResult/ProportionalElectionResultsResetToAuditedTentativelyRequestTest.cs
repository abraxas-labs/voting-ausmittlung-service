﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class ProportionalElectionResultsResetToAuditedTentativelyRequestTest : ProtoValidatorBaseTest<ProportionalElectionResultsResetToAuditedTentativelyRequest>
{
    protected override IEnumerable<ProportionalElectionResultsResetToAuditedTentativelyRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ElectionResultIds.Clear());
    }

    protected override IEnumerable<ProportionalElectionResultsResetToAuditedTentativelyRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultIds.Add("invalid-guid"));
        yield return NewValidRequest(x => x.ElectionResultIds.Add(string.Empty));
    }

    private ProportionalElectionResultsResetToAuditedTentativelyRequest NewValidRequest(Action<ProportionalElectionResultsResetToAuditedTentativelyRequest>? action = null)
    {
        var request = new ProportionalElectionResultsResetToAuditedTentativelyRequest
        {
            ElectionResultIds =
            {
                "f67b688a-0566-4e3c-bd73-6063834fedaf",
                "87a982e0-b8df-4318-b787-28a0c01693a4",
            },
        };

        action?.Invoke(request);
        return request;
    }
}
