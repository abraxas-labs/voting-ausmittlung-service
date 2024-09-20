// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ContactPerson;

public class EnterContactPersonRequestTest : ProtoValidatorBaseTest<EnterContactPersonRequest>
{
    public static EnterContactPersonRequest NewValidRequest(Action<EnterContactPersonRequest>? action = null)
    {
        var request = new EnterContactPersonRequest
        {
            FirstName = "Max",
            FamilyName = "Mustermann",
            Phone = "058 660 00 00",
            MobilePhone = "079 123 41 00",
            Email = "contact@abraxas.ch",
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterContactPersonRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.FirstName = "A");
        yield return NewValidRequest(x => x.FirstName = new string('A', 50));
        yield return NewValidRequest(x => x.FamilyName = "A");
        yield return NewValidRequest(x => x.FamilyName = new string('A', 50));
        yield return NewValidRequest(x => x.Email = string.Empty);
    }

    protected override IEnumerable<EnterContactPersonRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.FirstName = string.Empty);
        yield return NewValidRequest(x => x.FirstName = "M\nax");
        yield return NewValidRequest(x => x.FirstName = new string('A', 51));
        yield return NewValidRequest(x => x.FamilyName = string.Empty);
        yield return NewValidRequest(x => x.FamilyName = "Musz\ntermann");
        yield return NewValidRequest(x => x.FamilyName = new string('A', 51));
        yield return NewValidRequest(x => x.Phone = string.Empty);
        yield return NewValidRequest(x => x.MobilePhone = string.Empty);
        yield return NewValidRequest(x => x.MobilePhone = "+41 79 123 41 a0");
    }
}
