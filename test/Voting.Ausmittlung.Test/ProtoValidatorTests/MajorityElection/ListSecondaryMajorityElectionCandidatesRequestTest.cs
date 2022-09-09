// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElection;

public class ListSecondaryMajorityElectionCandidatesRequestTest : ProtoValidatorBaseTest<ListSecondaryMajorityElectionCandidatesRequest>
{
    protected override IEnumerable<ListSecondaryMajorityElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<ListSecondaryMajorityElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.SecondaryElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.SecondaryElectionId = string.Empty);
    }

    private ListSecondaryMajorityElectionCandidatesRequest NewValidRequest(Action<ListSecondaryMajorityElectionCandidatesRequest>? action = null)
    {
        var request = new ListSecondaryMajorityElectionCandidatesRequest
        {
            SecondaryElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
        };

        action?.Invoke(request);
        return request;
    }
}
