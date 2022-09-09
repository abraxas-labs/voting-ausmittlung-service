// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Test.ProtoValidatorTests.CountOfVoters;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.MajorityElectionResult;

public class EnterMajorityElectionCandidateResultsRequestTest : ProtoValidatorBaseTest<EnterMajorityElectionCandidateResultsRequest>
{
    public static EnterMajorityElectionCandidateResultsRequest NewValidRequest(Action<EnterMajorityElectionCandidateResultsRequest>? action = null)
    {
        var request = new EnterMajorityElectionCandidateResultsRequest
        {
            ElectionResultId = "f67b688a-0566-4e3c-bd73-6063834fedaf",
            CountOfVoters = EnterPoliticalBusinessCountOfVotersRequestTest.NewValidRequest(),
            IndividualVoteCount = 3,
            InvalidVoteCount = 4,
            EmptyVoteCount = 3,
            CandidateResults =
            {
                EnterMajorityElectionCandidateResultRequestTest.NewValidRequest(),
            },
            SecondaryElectionCandidateResults =
            {
                EnterSecondaryMajorityElectionCandidateResultsRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<EnterMajorityElectionCandidateResultsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.IndividualVoteCount = 0);
        yield return NewValidRequest(x => x.IndividualVoteCount = null);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000000);
        yield return NewValidRequest(x => x.EmptyVoteCount = 0);
        yield return NewValidRequest(x => x.EmptyVoteCount = null);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000000);
        yield return NewValidRequest(x => x.InvalidVoteCount = 0);
        yield return NewValidRequest(x => x.InvalidVoteCount = null);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000000);
    }

    protected override IEnumerable<EnterMajorityElectionCandidateResultsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ElectionResultId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionResultId = string.Empty);
        yield return NewValidRequest(x => x.CountOfVoters = null);
        yield return NewValidRequest(x => x.IndividualVoteCount = -1);
        yield return NewValidRequest(x => x.IndividualVoteCount = 1000001);
        yield return NewValidRequest(x => x.EmptyVoteCount = -1);
        yield return NewValidRequest(x => x.EmptyVoteCount = 1000001);
        yield return NewValidRequest(x => x.InvalidVoteCount = -1);
        yield return NewValidRequest(x => x.InvalidVoteCount = 1000001);
    }
}
