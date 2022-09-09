// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResultBundle;

public class CreateUpdateVoteResultBallotQuestionAnswerRequestTest : ProtoValidatorBaseTest<CreateUpdateVoteResultBallotQuestionAnswerRequest>
{
    public static CreateUpdateVoteResultBallotQuestionAnswerRequest NewValidRequest(Action<CreateUpdateVoteResultBallotQuestionAnswerRequest>? action = null)
    {
        var request = new CreateUpdateVoteResultBallotQuestionAnswerRequest
        {
            QuestionNumber = 2,
            Answer = BallotQuestionAnswer.Yes,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<CreateUpdateVoteResultBallotQuestionAnswerRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.QuestionNumber = 1);
        yield return NewValidRequest(x => x.QuestionNumber = 50);
        yield return NewValidRequest(x => x.Answer = BallotQuestionAnswer.Unspecified);
    }

    protected override IEnumerable<CreateUpdateVoteResultBallotQuestionAnswerRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.QuestionNumber = 0);
        yield return NewValidRequest(x => x.QuestionNumber = 51);
        yield return NewValidRequest(x => x.Answer = (BallotQuestionAnswer)5);
    }
}
