// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class EnterVoteBallotQuestionResultRequestTest : ProtoValidatorBaseTest<EnterVoteBallotQuestionResultRequest>
{
    public static EnterVoteBallotQuestionResultRequest NewValidRequest(Action<EnterVoteBallotQuestionResultRequest>? action = null)
    {
        var request = new EnterVoteBallotQuestionResultRequest
        {
            QuestionNumber = 3,
            ReceivedCountYes = 100,
            ReceivedCountNo = 20,
            ReceivedCountUnspecified = 1,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterVoteBallotQuestionResultRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.QuestionNumber = 1);
        yield return NewValidRequest(x => x.QuestionNumber = 50);
        yield return NewValidRequest(x => x.ReceivedCountYes = 0);
        yield return NewValidRequest(x => x.ReceivedCountYes = 1000000);
        yield return NewValidRequest(x => x.ReceivedCountNo = 0);
        yield return NewValidRequest(x => x.ReceivedCountNo = 1000000);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = 0);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = 1000000);
        yield return NewValidRequest(x => x.ReceivedCountYes = null);
        yield return NewValidRequest(x => x.ReceivedCountNo = null);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = null);
    }

    protected override IEnumerable<EnterVoteBallotQuestionResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.QuestionNumber = 0);
        yield return NewValidRequest(x => x.QuestionNumber = 51);
        yield return NewValidRequest(x => x.ReceivedCountYes = -1);
        yield return NewValidRequest(x => x.ReceivedCountYes = 1000001);
        yield return NewValidRequest(x => x.ReceivedCountNo = -1);
        yield return NewValidRequest(x => x.ReceivedCountNo = 1000001);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = -1);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = 1000001);
    }
}
