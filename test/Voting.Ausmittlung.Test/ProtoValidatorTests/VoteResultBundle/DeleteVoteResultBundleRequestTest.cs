// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VoteResultBundle;

public class DeleteVoteResultBundleRequestTest : ProtoValidatorBaseTest<DeleteVoteResultBundleRequest>
{
    protected override IEnumerable<DeleteVoteResultBundleRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<DeleteVoteResultBundleRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.BundleId = "invalid-guid");
        yield return NewValidRequest(x => x.BundleId = string.Empty);
        yield return NewValidRequest(x => x.BallotResultId = "invalid-guid");
        yield return NewValidRequest(x => x.BallotResultId = string.Empty);
    }

    private DeleteVoteResultBundleRequest NewValidRequest(Action<DeleteVoteResultBundleRequest>? action = null)
    {
        var request = new DeleteVoteResultBundleRequest
        {
            BundleId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            BallotResultId = "036be821-1a47-481e-a28c-2196cb1f14d0",
        };

        action?.Invoke(request);
        return request;
    }
}
