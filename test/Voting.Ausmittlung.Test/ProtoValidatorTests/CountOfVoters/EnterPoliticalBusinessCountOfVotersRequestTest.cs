// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.CountOfVoters;

public class EnterPoliticalBusinessCountOfVotersRequestTest : ProtoValidatorBaseTest<EnterPoliticalBusinessCountOfVotersRequest>
{
    public static EnterPoliticalBusinessCountOfVotersRequest NewValidRequest(Action<EnterPoliticalBusinessCountOfVotersRequest>? action = null)
    {
        var request = new EnterPoliticalBusinessCountOfVotersRequest
        {
            ConventionalReceivedBallots = 200,
            ConventionalAccountedBallots = 100,
            ConventionalBlankBallots = 50,
            ConventionalInvalidBallots = 50,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterPoliticalBusinessCountOfVotersRequest> OkMessages()
    {
        yield return NewValidRequest(x => x.ConventionalReceivedBallots = null);
        yield return NewValidRequest(x => x.ConventionalAccountedBallots = null);
        yield return NewValidRequest(x => x.ConventionalBlankBallots = null);
        yield return NewValidRequest(x => x.ConventionalInvalidBallots = null);

        yield return NewValidRequest(x => x.ConventionalReceivedBallots = 0);
        yield return NewValidRequest(x => x.ConventionalAccountedBallots = 0);
        yield return NewValidRequest(x => x.ConventionalBlankBallots = 0);
        yield return NewValidRequest(x => x.ConventionalInvalidBallots = 0);

        yield return NewValidRequest(x => x.ConventionalReceivedBallots = 1000000);
        yield return NewValidRequest(x => x.ConventionalAccountedBallots = 1000000);
        yield return NewValidRequest(x => x.ConventionalBlankBallots = 1000000);
        yield return NewValidRequest(x => x.ConventionalInvalidBallots = 1000000);
    }

    protected override IEnumerable<EnterPoliticalBusinessCountOfVotersRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ConventionalReceivedBallots = -1);
        yield return NewValidRequest(x => x.ConventionalAccountedBallots = -1);
        yield return NewValidRequest(x => x.ConventionalBlankBallots = -1);
        yield return NewValidRequest(x => x.ConventionalInvalidBallots = -1);

        yield return NewValidRequest(x => x.ConventionalReceivedBallots = 1000001);
        yield return NewValidRequest(x => x.ConventionalAccountedBallots = 1000001);
        yield return NewValidRequest(x => x.ConventionalBlankBallots = 1000001);
        yield return NewValidRequest(x => x.ConventionalInvalidBallots = 1000001);
    }
}
