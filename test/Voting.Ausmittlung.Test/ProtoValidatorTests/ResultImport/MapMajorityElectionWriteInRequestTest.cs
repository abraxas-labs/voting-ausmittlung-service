// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ResultImport;

public class MapMajorityElectionWriteInRequestTest : ProtoValidatorBaseTest<MapMajorityElectionWriteInRequest>
{
    public static MapMajorityElectionWriteInRequest NewValidRequest(Action<MapMajorityElectionWriteInRequest>? action = null)
    {
        var request = new MapMajorityElectionWriteInRequest
        {
            WriteInId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
            Target = MajorityElectionWriteInMappingTarget.Individual,
            CandidateId = "804602d7-3e1d-47c4-857e-9929fe792e1a",
        };

        action?.Invoke(request);
        return request;
    }

    protected override IEnumerable<MapMajorityElectionWriteInRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.CandidateId = string.Empty);
    }

    protected override IEnumerable<MapMajorityElectionWriteInRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.WriteInId = "invalid-guid");
        yield return NewValidRequest(x => x.WriteInId = string.Empty);
        yield return NewValidRequest(x => x.CandidateId = "invalid-guid");
        yield return NewValidRequest(x => x.Target = MajorityElectionWriteInMappingTarget.Unspecified);
        yield return NewValidRequest(x => x.Target = (MajorityElectionWriteInMappingTarget)99);
    }
}
