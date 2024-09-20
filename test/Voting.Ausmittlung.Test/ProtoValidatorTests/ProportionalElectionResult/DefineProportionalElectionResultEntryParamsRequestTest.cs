﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ProportionalElectionResult;

public class DefineProportionalElectionResultEntryParamsRequestTest : ProtoValidatorBaseTest<DefineProportionalElectionResultEntryParamsRequest>
{
    public static DefineProportionalElectionResultEntryParamsRequest NewValidRequest(Action<DefineProportionalElectionResultEntryParamsRequest>? action = null)
    {
        var request = new DefineProportionalElectionResultEntryParamsRequest
        {
            BallotBundleSampleSize = 2,
            BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
            AutomaticBallotBundleNumberGeneration = true,
            AutomaticEmptyVoteCounting = true,
            BallotBundleSize = 5,
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            CandidateCheckDigit = true,
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<DefineProportionalElectionResultEntryParamsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.BallotBundleSize = 1);
        yield return NewValidRequest(x => x.BallotBundleSize = 500);
        yield return NewValidRequest(x => x.BallotBundleSampleSize = 1);
        yield return NewValidRequest(x => x.BallotBundleSampleSize = 500);
        yield return NewValidRequest(x => x.AutomaticEmptyVoteCounting = false);
        yield return NewValidRequest(x => x.AutomaticBallotBundleNumberGeneration = false);
        yield return NewValidRequest(x => x.CandidateCheckDigit = false);
    }

    protected override IEnumerable<DefineProportionalElectionResultEntryParamsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BallotBundleSize = 0);
        yield return NewValidRequest(x => x.BallotBundleSize = 501);
        yield return NewValidRequest(x => x.BallotBundleSampleSize = 0);
        yield return NewValidRequest(x => x.BallotBundleSampleSize = 501);
        yield return NewValidRequest(x => x.BallotNumberGeneration = BallotNumberGeneration.Unspecified);
        yield return NewValidRequest(x => x.BallotNumberGeneration = (BallotNumberGeneration)12);
        yield return NewValidRequest(x => x.ReviewProcedure = ProportionalElectionReviewProcedure.Unspecified);
        yield return NewValidRequest(x => x.ReviewProcedure = (ProportionalElectionReviewProcedure)12);
    }
}
