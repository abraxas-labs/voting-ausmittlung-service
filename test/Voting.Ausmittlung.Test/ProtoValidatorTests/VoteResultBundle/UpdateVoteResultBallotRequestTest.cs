﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResultBundle;

public class UpdateVoteResultBallotRequestTest : ProtoValidatorBaseTest<UpdateVoteResultBallotRequest>
{
    protected override IEnumerable<UpdateVoteResultBallotRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BallotNumber = 1);
        yield return NewValidRequest(x => x.BallotNumber = 1000000);
    }

    protected override IEnumerable<UpdateVoteResultBallotRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
        yield return NewValidRequest(x => x.BallotNumber = 0);
        yield return NewValidRequest(x => x.BallotNumber = 1000001);
    }

    private UpdateVoteResultBallotRequest NewValidRequest(Action<UpdateVoteResultBallotRequest>? action = null)
    {
        var request = new UpdateVoteResultBallotRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            BallotNumber = 2,
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
