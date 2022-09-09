// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.VotingCards;

public class UpdateVotingCardResultDetailRequestTest : ProtoValidatorBaseTest<UpdateVotingCardResultDetailRequest>
{
    protected override IEnumerable<UpdateVotingCardResultDetailRequest> OkMessages()
    {
        yield return NewValidRequest(x => x.CountOfReceivedVotingCards = 0);
        yield return NewValidRequest(x => x.CountOfReceivedVotingCards = null);
        yield return NewValidRequest(x => x.CountOfReceivedVotingCards = 1000000);
    }

    protected override IEnumerable<UpdateVotingCardResultDetailRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.CountOfReceivedVotingCards = -1);
        yield return NewValidRequest(x => x.CountOfReceivedVotingCards = 1000001);
        yield return NewValidRequest(x => x.Channel = VotingChannel.Unspecified);
        yield return NewValidRequest(x => x.Channel = (VotingChannel)(-1));
        yield return NewValidRequest(x => x.DomainOfInfluenceType = DomainOfInfluenceType.Unspecified);
        yield return NewValidRequest(x => x.DomainOfInfluenceType = (DomainOfInfluenceType)101);
    }

    private UpdateVotingCardResultDetailRequest NewValidRequest(Action<UpdateVotingCardResultDetailRequest>? action = null)
    {
        var request = new UpdateVotingCardResultDetailRequest
        {
            CountOfReceivedVotingCards = 1,
            Channel = VotingChannel.BallotBox,
            Valid = true,
            DomainOfInfluenceType = DomainOfInfluenceType.Ct,
        };

        action?.Invoke(request);
        return request;
    }
}
