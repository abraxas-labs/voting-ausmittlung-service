// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElection;

public class ListMajorityElectionCandidatesRequestTest : ProtoValidatorBaseTest<ListMajorityElectionCandidatesRequest>
{
    protected override IEnumerable<ListMajorityElectionCandidatesRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.IncludeCandidatesOfSecondaryElection = false);
    }

    protected override IEnumerable<ListMajorityElectionCandidatesRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionId = string.Empty);
    }

    private ListMajorityElectionCandidatesRequest NewValidRequest(Action<ListMajorityElectionCandidatesRequest>? action = null)
    {
        var request = new ListMajorityElectionCandidatesRequest
        {
            ElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            IncludeCandidatesOfSecondaryElection = true,
        };

        action?.Invoke(request);
        return request;
    }
}
