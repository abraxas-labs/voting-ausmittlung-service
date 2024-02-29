// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResultBundle;

public class CreateVoteResultBallotRequestTest : ProtoValidatorBaseTest<CreateVoteResultBallotRequest>
{
    protected override IEnumerable<CreateVoteResultBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<CreateVoteResultBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
    }

    private CreateVoteResultBallotRequest NewValidRequest(Action<CreateVoteResultBallotRequest>? action = null)
    {
        var request = new CreateVoteResultBallotRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            QuestionAnswers =
            {
                CreateUpdateVoteResultBallotQuestionAnswerRequestTest.NewValidRequest(),
            },
            TieBreakQuestionAnswers =
            {
                CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
