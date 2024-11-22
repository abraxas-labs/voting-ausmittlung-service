// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.ProtoValidatorTests.ContactPerson;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ContestCountingCircleContactPerson;

public class UpdateContestCountingCircleContactPersonRequestTest : ProtoValidatorBaseTest<UpdateContestCountingCircleContactPersonRequest>
{
    protected override IEnumerable<UpdateContestCountingCircleContactPersonRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ContactPersonAfterEvent = null);
    }

    protected override IEnumerable<UpdateContestCountingCircleContactPersonRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Id = string.Empty);
        yield return NewValidRequest(x => x.Id = "invalid-guid");
        yield return NewValidRequest(x => x.ContactPersonDuringEvent = null);
    }

    private UpdateContestCountingCircleContactPersonRequest NewValidRequest(Action<UpdateContestCountingCircleContactPersonRequest>? action = null)
    {
        var request = new UpdateContestCountingCircleContactPersonRequest
        {
            Id = "636d68d0-6654-4c07-9610-56dad5df20bd",
            ContactPersonDuringEvent = EnterContactPersonRequestTest.NewValidRequest(x => x.FirstName = "Ali (Bart)"),
            ContactPersonAfterEvent = EnterContactPersonRequestTest.NewValidRequest(x => x.FirstName = "Hans"),
        };

        action?.Invoke(request);
        return request;
    }
}
