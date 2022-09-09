// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ResultImport;

public class MapMajorityElectionWriteInsRequestTest : ProtoValidatorBaseTest<MapMajorityElectionWriteInsRequest>
{
    protected override IEnumerable<MapMajorityElectionWriteInsRequest> OkMessages()
    {
        yield return NewValidRequest();
    }

    protected override IEnumerable<MapMajorityElectionWriteInsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ImportId = "invalid-guid");
        yield return NewValidRequest(x => x.ImportId = string.Empty);
        yield return NewValidRequest(x => x.ElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.ElectionId = string.Empty);
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.PoliticalBusinessType = PoliticalBusinessType.Unspecified);
        yield return NewValidRequest(x => x.PoliticalBusinessType = (PoliticalBusinessType)6);
    }

    private MapMajorityElectionWriteInsRequest NewValidRequest(Action<MapMajorityElectionWriteInsRequest>? action = null)
    {
        var request = new MapMajorityElectionWriteInsRequest
        {
            ImportId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
            CountingCircleId = "bd1167e9-0081-4472-af02-ebd799e0000c",
            ElectionId = "30b88cf7-68cb-416d-80be-63ed3dae0271",
            PoliticalBusinessType = PoliticalBusinessType.Vote,
            Mappings =
            {
                MapMajorityElectionWriteInRequestTest.NewValidRequest(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
