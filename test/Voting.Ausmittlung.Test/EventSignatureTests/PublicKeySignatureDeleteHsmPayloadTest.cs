// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.EventSignatureTests;

public class PublicKeySignatureDeleteHsmPayloadTest
{
    [Fact]
    public void SignatureShouldStayConsistent()
    {
        var payload = new PublicKeySignatureDeleteHsmPayload(
            1,
            Guid.Parse("5ace0407-261f-4ae7-a901-b0adaf54179e"),
            "my-host",
            "my-key",
            MockedClock.GetDate(-10),
            11,
            new byte[] { 0x10, 0x20, 0xFF });
        var bytesToSign = Convert.ToBase64String(payload.ConvertToBytesToSign());
        bytesToSign.Should().Be("AAAAATVhY2UwNDA3LTI2MWYtNGFlNy1hOTAxLWIwYWRhZjU0MTc5ZW15LWhvc3RteS1rZXkAAAFvXBXM2AAAAAAAAAALNeM3dwVrOmUbQRNHFF9f3oke0QFgMgmYe1F7W5et4RqPy+kUrJz69s/hPdcAl5WKRGUQ8Kgi0E2xUAsKm2ExFg==");
    }
}
