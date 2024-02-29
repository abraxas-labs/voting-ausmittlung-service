// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.EventSignatureTests;

public class PublicKeySignatureCreateHsmPayloadTest
{
    [Fact]
    public void SignatureShouldStayConsistent()
    {
        var payload = new PublicKeySignatureCreateHsmPayload(
            1,
            Guid.Parse("5ace0407-261f-4ae7-a901-b0adaf54179e"),
            "my-host",
            "my-key",
            new byte[] { 0x10, 0x20, 0xFF },
            MockedClock.GetDate(-10),
            MockedClock.GetDate(10),
            new byte[] { 0x20, 0x30, 0xFF });
        var bytesToSign = Convert.ToBase64String(payload.ConvertToBytesToSign());
        bytesToSign.Should().Be("AAAAATVhY2UwNDA3LTI2MWYtNGFlNy1hOTAxLWIwYWRhZjU0MTc5ZW15LWhvc3RteS1rZXk14zd3BWs6ZRtBE0cUX1/eiR7RAWAyCZh7UXtbl63hGo/L6RSsnPr2z+E91wCXlYpEZRDwqCLQTbFQCwqbYTEWAAABb1wVzNgAAAFvwxT82L4Oui2+do24bkJT23GWiiN/rqfObc5E5stQKjuXuVB2RunNCrkWe9KO9SO4oRpHd94b+JLcA4xgsATBH4+5Ngg=");
    }
}
