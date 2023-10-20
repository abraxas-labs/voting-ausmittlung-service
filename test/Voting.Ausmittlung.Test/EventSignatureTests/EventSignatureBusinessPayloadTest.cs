// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file
using System;
using FluentAssertions;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.EventSignatureTests;

public class EventSignatureBusinessPayloadTest
{
    [Fact]
    public void SignatureShouldStayConsistent()
    {
        var payload = new EventSignatureBusinessPayload(
            1,
            Guid.Parse("5ace0407-261f-4ae7-a901-b0adaf54179e"),
            "my-stream",
            new byte[] { 0x10, 0x20, 0xFF },
            Guid.Parse("2a9a791a-2778-4fed-a70e-ac53dd839f84"),
            "host-id",
            "key-id",
            MockedClock.UtcNowDate);
        var bytesToSign = Convert.ToBase64String(payload.ConvertToBytesToSign());
        bytesToSign.Should().Be("AAAAATVhY2UwNDA3LTI2MWYtNGFlNy1hOTAxLWIwYWRhZjU0MTc5ZW15LXN0cmVhbRAg/zJhOWE3OTFhLTI3NzgtNGZlZC1hNzBlLWFjNTNkZDgzOWY4NGhvc3QtaWRrZXktaWQAAAFvj5Vk2A==");
    }
}
