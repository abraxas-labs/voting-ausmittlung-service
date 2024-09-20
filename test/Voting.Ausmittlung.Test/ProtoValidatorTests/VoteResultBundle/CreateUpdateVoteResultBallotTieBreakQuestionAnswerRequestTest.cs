// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResultBundle;

public class CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequestTest : ProtoValidatorBaseTest<CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest>
{
    public static CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest NewValidRequest(Action<CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest>? action = null)
    {
        var request = new CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest
        {
            QuestionNumber = 2,
            Answer = TieBreakQuestionAnswer.Q1,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.QuestionNumber = 1);
        yield return NewValidRequest(x => x.QuestionNumber = 50);
        yield return NewValidRequest(x => x.Answer = TieBreakQuestionAnswer.Unspecified);
    }

    protected override IEnumerable<CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.QuestionNumber = 0);
        yield return NewValidRequest(x => x.QuestionNumber = 51);
        yield return NewValidRequest(x => x.Answer = (TieBreakQuestionAnswer)5);
    }
}
