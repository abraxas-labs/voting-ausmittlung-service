// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.ProtoValidatorTests.ContactPerson;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ContestCountingCircleContactPerson;

public class CreateContestCountingCircleContactPersonRequestTest : ProtoValidatorBaseTest<CreateContestCountingCircleContactPersonRequest>
{
    protected override IEnumerable<CreateContestCountingCircleContactPersonRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ContactPersonAfterEvent = null);
    }

    protected override IEnumerable<CreateContestCountingCircleContactPersonRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContactPersonDuringEvent = null);
    }

    private CreateContestCountingCircleContactPersonRequest NewValidRequest(Action<CreateContestCountingCircleContactPersonRequest>? action = null)
    {
        var request = new CreateContestCountingCircleContactPersonRequest
        {
            CountingCircleId = "636d68d0-6654-4c07-9610-56dad5df20bd",
            ContestId = "b93ccec1-dbdf-4a00-9328-0a2026214b20",
            ContactPersonDuringEvent = EnterContactPersonRequestTest.NewValidRequest(x => x.FirstName = "Ali (Bart)"),
            ContactPersonAfterEvent = EnterContactPersonRequestTest.NewValidRequest(x => x.FirstName = "Hans"),
        };

        action?.Invoke(request);
        return request;
    }
}
