// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.EventSignatureTests;

public class PublicKeySignatureCreateAuthenticationTagPayloadTest
{
    [Fact]
    public void SignatureShouldStayConsistent()
    {
        var payload = new PublicKeySignatureCreateAuthenticationTagPayload(
            1,
            Guid.Parse("5ace0407-261f-4ae7-a901-b0adaf54179e"),
            "my-host",
            "my-key",
            new byte[] { 0x10, 0x20, 0xFF },
            MockedClock.GetDate(-10),
            MockedClock.GetDate(10));
        var bytesToSign = Convert.ToBase64String(payload.ConvertToBytesToSign());
        bytesToSign.Should().Be("AAAAATVhY2UwNDA3LTI2MWYtNGFlNy1hOTAxLWIwYWRhZjU0MTc5ZW15LWhvc3RteS1rZXk14zd3BWs6ZRtBE0cUX1/eiR7RAWAyCZh7UXtbl63hGo/L6RSsnPr2z+E91wCXlYpEZRDwqCLQTbFQCwqbYTEWAAABb1wVzNgAAAFvwxT82A==");
    }
}
