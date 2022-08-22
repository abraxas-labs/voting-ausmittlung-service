// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Lib.Cryptography.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class PublicKeySignaturePayloadTest
{
    [Fact]
    public void ShouldReturnV1PayloadBytes()
    {
        var contestId = Guid.Parse("b0e1da98-b994-4391-860f-7bd4f7e6ccaa");
        var hostId = "Test-Host";
        var key = new AsymmetricAlgorithmAdapterMock().CreateRandomPrivateKey();

        // use a mocked key id, because it is random.
        var keyId = "+9ykQM7NWXBTHAwBuHe/yw==";
        keyId.Should().HaveLength(key.Id.Length);

        var validFrom = new DateTime(2022, 4, 29, 12, 0, 0, DateTimeKind.Utc);
        var validTo = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var publicKeySignaturePayload = new PublicKeySignaturePayload(
            EventSignatureVersions.V1,
            contestId,
            hostId,
            keyId,
            key.PublicKey,
            validFrom,
            validTo);

        var result = Convert.ToBase64String(publicKeySignaturePayload.ConvertToBytesToSign());
        var expectedResult = "AAAAAWIwZTFkYTk4LWI5OTQtNDM5MS04NjBmLTdiZDRmN2U2Y2NhYVRlc3QtSG9zdCs5eWtRTTdOV1hCVEhBd0J1SGUveXc9PYC8dkQ/y7PJ69tJwARnpJxPEhYH+obUnnklxuZ52mm/0WniWbvSmJrTymU1e5phRNcmiybP6KFZgsMomO58hdAAAAGAdTEyAAAAAYEcj+AA";
        result.Should().Be(expectedResult);
    }
}
