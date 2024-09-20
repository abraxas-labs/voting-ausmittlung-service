// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class EnterSecondaryMajorityElectionCandidateResultsRequestTest : ProtoValidatorBaseTest<EnterSecondaryMajorityElectionCandidateResultsRequest>
{
    public static EnterSecondaryMajorityElectionCandidateResultsRequest NewValidRequest(Action<EnterSecondaryMajorityElectionCandidateResultsRequest>? action = null)
    {
        var request = new EnterSecondaryMajorityElectionCandidateResultsRequest
        {
            SecondaryMajorityElectionId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            EmptyVoteCount = 12,
            IndividualVoteCount = 9,
            InvalidVoteCount = 6,
            CandidateResults =
            {
                EnterMajorityElectionCandidateResultRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterSecondaryMajorityElectionCandidateResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.EmptyVoteCount = 0);
        yield return NewValidRequest(x => x.EmptyVoteCount = null);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000000);
        yield return NewValidRequest(x => x.IndividualVoteCount = 0);
        yield return NewValidRequest(x => x.IndividualVoteCount = null);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000000);
        yield return NewValidRequest(x => x.InvalidVoteCount = 0);
        yield return NewValidRequest(x => x.InvalidVoteCount = null);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000000);
        yield return NewValidRequest(x => x.CandidateResults.Clear());
    }

    protected override IEnumerable<EnterSecondaryMajorityElectionCandidateResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.SecondaryMajorityElectionId = string.Empty);
        yield return NewValidRequest(x => x.EmptyVoteCount = -1);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000001);
        yield return NewValidRequest(x => x.IndividualVoteCount = -1);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000001);
        yield return NewValidRequest(x => x.InvalidVoteCount = -1);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000001);
    }
}
