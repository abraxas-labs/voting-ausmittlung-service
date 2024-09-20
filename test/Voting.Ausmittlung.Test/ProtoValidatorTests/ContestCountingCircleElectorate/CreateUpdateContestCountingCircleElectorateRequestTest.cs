// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ContestCountingCircleElectorate;

public class CreateUpdateContestCountingCircleElectorateRequestTest : ProtoValidatorBaseTest<CreateUpdateContestCountingCircleElectorateRequest>
{
    public static CreateUpdateContestCountingCircleElectorateRequest NewValidRequest(Action<CreateUpdateContestCountingCircleElectorateRequest>? action = null)
    {
        var request = new CreateUpdateContestCountingCircleElectorateRequest
        {
            DomainOfInfluenceTypes = { DomainOfInfluenceType.Ct, DomainOfInfluenceType.Ch, },
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<CreateUpdateContestCountingCircleElectorateRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<CreateUpdateContestCountingCircleElectorateRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.DomainOfInfluenceTypes.Add(DomainOfInfluenceType.Unspecified));
        yield return NewValidRequest(x => x.DomainOfInfluenceTypes.Add((DomainOfInfluenceType)15));
    }
}
