// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResult;

public class DefineVoteResultEntryParamsRequestTest : ProtoValidatorBaseTest<DefineVoteResultEntryParamsRequest>
{
    public static DefineVoteResultEntryParamsRequest NewValidRequest(Action<DefineVoteResultEntryParamsRequest>? action = null)
    {
        var request = new DefineVoteResultEntryParamsRequest
        {
            BallotBundleSampleSizePercent = 20,
            AutomaticBallotBundleNumberGeneration = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<DefineVoteResultEntryParamsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BallotBundleSampleSizePercent = 1);
        yield return NewValidRequest(x => x.BallotBundleSampleSizePercent = 100);
        yield return NewValidRequest(x => x.AutomaticBallotBundleNumberGeneration = false);
    }

    protected override IEnumerable<DefineVoteResultEntryParamsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BallotBundleSampleSizePercent = 0);
        yield return NewValidRequest(x => x.BallotBundleSampleSizePercent = 101);
        yield return NewValidRequest(x => x.ReviewProcedure = VoteReviewProcedure.Unspecified);
        yield return NewValidRequest(x => x.ReviewProcedure = (VoteReviewProcedure)12);
    }
}
