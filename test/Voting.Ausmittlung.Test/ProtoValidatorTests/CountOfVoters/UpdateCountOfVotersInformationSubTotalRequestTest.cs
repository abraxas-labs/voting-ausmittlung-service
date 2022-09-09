// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.CountOfVoters;

public class UpdateCountOfVotersInformationSubTotalRequestTest : ProtoValidatorBaseTest<UpdateCountOfVotersInformationSubTotalRequest>
{
    protected override IEnumerable<UpdateCountOfVotersInformationSubTotalRequest> OkMessages()
    {
        yield return NewValidRequest(x => x.Sex = SexType.Undefined);
        yield return NewValidRequest(x => x.CountOfVoters = 0);
        yield return NewValidRequest(x => x.CountOfVoters = null);
        yield return NewValidRequest(x => x.CountOfVoters = 1000000);
    }

    protected override IEnumerable<UpdateCountOfVotersInformationSubTotalRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.Sex = SexType.Unspecified);
        yield return NewValidRequest(x => x.Sex = (SexType)6);
        yield return NewValidRequest(x => x.VoterType = VoterType.Unspecified);
        yield return NewValidRequest(x => x.VoterType = (VoterType)(-1));
        yield return NewValidRequest(x => x.CountOfVoters = -1);
        yield return NewValidRequest(x => x.CountOfVoters = 1000001);
    }

    private UpdateCountOfVotersInformationSubTotalRequest NewValidRequest(Action<UpdateCountOfVotersInformationSubTotalRequest>? action = null)
    {
        var request = new UpdateCountOfVotersInformationSubTotalRequest
        {
            Sex = SexType.Male,
            VoterType = VoterType.SwissAbroad,
            CountOfVoters = 1,
        };

        action?.Invoke(request);
        return request;
    }
}
