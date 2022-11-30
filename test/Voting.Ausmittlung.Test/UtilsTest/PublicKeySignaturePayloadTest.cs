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
    private static readonly Guid ContestId = Guid.Parse("b0e1da98-b994-4391-860f-7bd4f7e6ccaa");
    private static readonly string HostId = "Test-Host";

    // use a mocked key id, because it is random.
    private static readonly string KeyId = "+9ykQM7NWXBTHAwBuHe/yw==";

    [Fact]
    public void ShouldReturnV1PublicKeySignatureCreateAuthenticationTagPayloadBytes()
    {
        var key = new AsymmetricAlgorithmAdapterMock().CreateRandomPrivateKey();
        KeyId.Should().HaveLength(key.Id.Length);

        var publicKeySignatureAuthTagPayload = new PublicKeySignatureCreateAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestId,
            HostId,
            KeyId,
            key.PublicKey,
            new DateTime(2022, 4, 29, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = Convert.ToBase64String(publicKeySignatureAuthTagPayload.ConvertToBytesToSign());
        var expectedResult = "AAAAAWIwZTFkYTk4LWI5OTQtNDM5MS04NjBmLTdiZDRmN2U2Y2NhYVRlc3QtSG9zdCs5eWtRTTdOV1hCVEhBd0J1SGUveXc9PYC8dkQ/y7PJ69tJwARnpJxPEhYH+obUnnklxuZ52mm/0WniWbvSmJrTymU1e5phRNcmiybP6KFZgsMomO58hdAAAAGAdTEyAAAAAYEcj+AA";
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ShouldReturnV1PublicKeySignatureCreateHsmPayloadBytes()
    {
        var key = new AsymmetricAlgorithmAdapterMock().CreateRandomPrivateKey();

        var publicKeySignatureAuthTagPayload = new PublicKeySignatureCreateAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestId,
            HostId,
            KeyId,
            key.PublicKey,
            new DateTime(2022, 4, 29, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var publicKeySignatureHsmPayload = new PublicKeySignatureCreateHsmPayload(
            publicKeySignatureAuthTagPayload,
            Convert.FromBase64String("AduyaSZ3QeQngL9hiBXg5CyEd+dzdvCqKni+D8cwyCxiTZHkbjzM7TZCJEt92OHphCJrrEuXlW9LSUPXoh/DEkewAdrZRG/Pt1zhqs2GJTr36zDw4HgR3QF88E6aRpWKCrqA841fH9fZU7TayXYfm11/CAMPPM2g6A1cDT/HRC9TNmsS"));

        var result = Convert.ToBase64String(publicKeySignatureHsmPayload.ConvertToBytesToSign());
        var expectedResult = "AAAAAWIwZTFkYTk4LWI5OTQtNDM5MS04NjBmLTdiZDRmN2U2Y2NhYVRlc3QtSG9zdCs5eWtRTTdOV1hCVEhBd0J1SGUveXc9PYC8dkQ/y7PJ69tJwARnpJxPEhYH+obUnnklxuZ52mm/0WniWbvSmJrTymU1e5phRNcmiybP6KFZgsMomO58hdAAAAGAdTEyAAAAAYEcj+AAIGAQWsLlY74baaikL0K2BQo3Eps7CKuDeUCoprdt12axecA1MH7trfmSUS605I2kmptbdx1WNtUEnJd0/GVoDw==";
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ShouldReturnV1PublicKeySignatureDeleteAuthenticationTagPayloadBytes()
    {
        var publicKeySignatureAuthTagPayload = new PublicKeySignatureDeleteAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestId,
            HostId,
            KeyId,
            new DateTime(2022, 4, 29, 12, 0, 0, DateTimeKind.Utc),
            101);

        var result = Convert.ToBase64String(publicKeySignatureAuthTagPayload.ConvertToBytesToSign());
        var expectedResult = "AAAAAWIwZTFkYTk4LWI5OTQtNDM5MS04NjBmLTdiZDRmN2U2Y2NhYVRlc3QtSG9zdCs5eWtRTTdOV1hCVEhBd0J1SGUveXc9PQAAAYB1MTIAAAAAAAAAAGU=";
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ShouldReturnV1PublicKeySignatureDeleteHsmPayloadBytes()
    {
        var publicKeySignatureAuthTagPayload = new PublicKeySignatureDeleteAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestId,
            HostId,
            KeyId,
            new DateTime(2022, 4, 29, 12, 0, 0, DateTimeKind.Utc),
            101);

        var publicKeySignatureHsmPayload = new PublicKeySignatureDeleteHsmPayload(
            publicKeySignatureAuthTagPayload,
            Convert.FromBase64String("AduyaSZ3QeQngL9hiBXg5CyEd+dzdvCqKni+D8cwyCxiTZHkbjzM7TZCJEt92OHphCJrrEuXlW9LSUPXoh/DEkewAdrZRG/Pt1zhqs2GJTr36zDw4HgR3QF88E6aRpWKCrqA841fH9fZU7TayXYfm11/CAMPPM2g6A1cDT/HRC9TNmsS"));

        var result = Convert.ToBase64String(publicKeySignatureHsmPayload.ConvertToBytesToSign());
        var expectedResult = "AAAAAWIwZTFkYTk4LWI5OTQtNDM5MS04NjBmLTdiZDRmN2U2Y2NhYVRlc3QtSG9zdCs5eWtRTTdOV1hCVEhBd0J1SGUveXc9PQAAAYB1MTIAAAAAAAAAAGUgYBBawuVjvhtpqKQvQrYFCjcSmzsIq4N5QKimt23XZrF5wDUwfu2t+ZJRLrTkjaSam1t3HVY21QScl3T8ZWgP";
        result.Should().Be(expectedResult);
    }
}
