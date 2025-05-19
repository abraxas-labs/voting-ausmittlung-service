// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Voting.Lib.Testing.Validation;

namespace Voting.Ausmittlung.Test.ProtoValidatorTests.ResultImport;

public class GetMajorityElectionWriteInMappingsRequestTest : ProtoValidatorBaseTest<GetMajorityElectionWriteInMappingsRequest>
{
    protected override IEnumerable<GetMajorityElectionWriteInMappingsRequest> OkMessages()
    {
        yield return NewValidRequest();
        yield return NewValidRequest(x => x.ImportType = ResultImportType.Unspecified);
        yield return NewValidRequest(x => x.ElectionId = string.Empty);
    }

    protected override IEnumerable<GetMajorityElectionWriteInMappingsRequest> NotOkMessages()
    {
        yield return NewValidRequest(x => x.ContestId = "invalid-guid");
        yield return NewValidRequest(x => x.ContestId = string.Empty);
        yield return NewValidRequest(x => x.ElectionId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = "invalid-guid");
        yield return NewValidRequest(x => x.CountingCircleId = string.Empty);
        yield return NewValidRequest(x => x.ImportType = (ResultImportType)(-1));
    }

    private GetMajorityElectionWriteInMappingsRequest NewValidRequest(Action<GetMajorityElectionWriteInMappingsRequest>? action = null)
    {
        var request = new GetMajorityElectionWriteInMappingsRequest
        {
            ContestId = "04a2aff6-240f-4496-9e97-29881e84a2d4",
            CountingCircleId = "bd1167e9-0081-4472-af02-ebd799e0000c",
            ElectionId = "771022bc-c2fe-4140-990a-b45c40c453ab",
            ImportType = ResultImportType.Ecounting,
        };

        action?.Invoke(request);
        return request;
    }
}
