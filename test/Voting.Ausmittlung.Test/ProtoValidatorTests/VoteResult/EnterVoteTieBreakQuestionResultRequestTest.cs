// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class EnterVoteTieBreakQuestionResultRequestTest : ProtoValidatorBaseTest<EnterVoteTieBreakQuestionResultRequest>
{
    public static EnterVoteTieBreakQuestionResultRequest NewValidRequest(Action<EnterVoteTieBreakQuestionResultRequest>? action = null)
    {
        var request = new EnterVoteTieBreakQuestionResultRequest
        {
            QuestionNumber = 3,
            ReceivedCountQ1 = 100,
            ReceivedCountQ2 = 20,
            ReceivedCountUnspecified = 1,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterVoteTieBreakQuestionResultRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.QuestionNumber = 1);
        yield return NewValidRequest(x => x.QuestionNumber = 50);
        yield return NewValidRequest(x => x.ReceivedCountQ1 = 0);
        yield return NewValidRequest(x => x.ReceivedCountQ1 = null);
        yield return NewValidRequest(x => x.ReceivedCountQ1 = 1000000);
        yield return NewValidRequest(x => x.ReceivedCountQ2 = 0);
        yield return NewValidRequest(x => x.ReceivedCountQ2 = null);
        yield return NewValidRequest(x => x.ReceivedCountQ2 = 1000000);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = 0);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = null);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = 1000000);
    }

    protected override IEnumerable<EnterVoteTieBreakQuestionResultRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.QuestionNumber = 0);
        yield return NewValidRequest(x => x.QuestionNumber = 51);
        yield return NewValidRequest(x => x.ReceivedCountQ1 = -1);
        yield return NewValidRequest(x => x.ReceivedCountQ1 = 1000001);
        yield return NewValidRequest(x => x.ReceivedCountQ2 = -1);
        yield return NewValidRequest(x => x.ReceivedCountQ2 = 1000001);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = -1);
        yield return NewValidRequest(x => x.ReceivedCountUnspecified = 1000001);
    }
}
