// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test.EventSignatureTests;

public class PublicKeySignatureVerifierTest : BaseTest<TestApplicationFactory, TestStartup>
{
    private readonly PublicKeySignatureVerifier _verifier;
    private readonly IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> _asymmetricAlgorithmAdapter;

    public PublicKeySignatureVerifierTest(TestApplicationFactory factory)
        : base(factory)
    {
        _verifier = GetService<PublicKeySignatureVerifier>();
        _asymmetricAlgorithmAdapter = GetService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
    }

    [Fact]
    public void TestValidSignature()
    {
        var result = _verifier.VerifySignature(NewCreateData(), NewDeleteData(), NewKeyData());
        result.SignatureData.Should().NotBeNull();
        result.CreatePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.Valid);
        result.DeletePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.Valid);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TestValidSignatureWithoutDeleteData()
    {
        var result = _verifier.VerifySignature(NewCreateData(), null, NewKeyData());
        result.SignatureData.Should().NotBeNull();
        result.CreatePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.Valid);
        result.DeletePublicKeySignatureValidationResultType.Should().BeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TestNotMatchingContestId()
    {
        var result = _verifier.VerifySignature(
            NewCreateData(x => x.ContestId = Guid.Empty.ToString()),
            NewDeleteData(),
            NewKeyData());
        result.SignatureData.Should().BeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TestNotMatchingKeyId()
    {
        var result = _verifier.VerifySignature(
            NewCreateData(x => x.KeyId = "Random"),
            NewDeleteData(),
            NewKeyData());
        result.SignatureData.Should().BeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TestNotMatchingSignatureVersion()
    {
        var result = _verifier.VerifySignature(
            NewCreateData(),
            NewDeleteData(x => x.SignatureVersion = 3),
            NewKeyData());
        result.SignatureData.Should().BeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TestInvalidAuthTagOnCreate()
    {
        var result = _verifier.VerifySignature(
            NewCreateData(x => x.AuthenticationTag = ConvertBase64StringToByteString("AFCXTVrm9CSvOhSOIT3fRXUaKWv0hpMKnfB4UjBYBHMY0f1/tnQJeSedWQtTeREbosUbJPQs8xLpE1nVTJaQ+FxIAY7EcyVoKJNmXkwqy9ObyZKPJzrYCD4gUD+u3AUbsOBwfPnakVX80QkWxl1HJe+C8IfjkfXOCXdqyqHtaksLFstx")),
            NewDeleteData(),
            NewKeyData());
        result.CreatePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.AuthenticationTagInvalid);
        result.DeletePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.Valid);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TestInvalidHsmSignatureOnCreate()
    {
        var result = _verifier.VerifySignature(
            NewCreateData(null, Convert.FromBase64String("AZChPYnXpkawnQWBpuChPUK826lecevTZmvM7CtN0rMXyhjRoJwbXGjJKky2Y7HRVVx7ii+PJpo5893nK3YpQHtaAUGZkttjJ5dOdzMbHyuU75eiddbUW2vQn/eRDgoHXv38T0fSyC7FD6iEkR53XEg0JlDvnDvYxZEkQGNbG9VrzYZV")),
            NewDeleteData(),
            NewKeyData());
        result.CreatePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.HsmSignatureInvalid);
        result.DeletePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.Valid);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TestInvalidAuthTagOnDelete()
    {
        var result = _verifier.VerifySignature(
            NewCreateData(),
            NewDeleteData(x => x.AuthenticationTag = ConvertBase64StringToByteString("AHEFnWNJv1dvQD3GpRNfHgacAiW7bjfS6XCl0j+6PhkwHh2hpobbgj0Hv/dx2qvW953Qr4SMEwEfKiJAGFhPXjA1AcMfHynNsD+T1CgUNipc8PZ8mZMgol0Eq4NBAXMuUnIoIe7cjni8FdRFIPU+NzBVKapr3vpz4iTjz4x9lY/oAyGZ")),
            NewKeyData());
        result.CreatePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.Valid);
        result.DeletePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.AuthenticationTagInvalid);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TestInvalidHsmSignatureOnDelete()
    {
        var result = _verifier.VerifySignature(
            NewCreateData(),
            NewDeleteData(null, Convert.FromBase64String("AHmdWmclPaF597VdtJRMYFY+KtseA4VljNPkg0IXsMM1B6bF48y1s2xT/RqwqMo9DkphC3v5+meUbcIK9RT4K0MWAEMXs4xshA35mTVf61b2IbHSRzeq4vsAD7VvPnI6j3I4iIusLNzScZUD/VLwroUx9CG+ONflusxcl1rRxWZxrHWn")),
            NewKeyData());
        result.CreatePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.Valid);
        result.DeletePublicKeySignatureValidationResultType.Should().Be(PublicKeySignatureValidationResultType.HsmSignatureInvalid);
        result.IsValid.Should().BeFalse();
    }

    private PublicKeySignatureCreateData NewCreateData(
        Action<EventSignaturePublicKeyCreated>? customizer = null,
        byte[]? hsmSignature = null)
    {
        var ev = new EventSignaturePublicKeyCreated
        {
            SignatureVersion = EventSignatureVersions.V1,
            ContestId = "e9840c66-9bd3-4950-9b22-d680fa6ba166",
            HostId = "Host",
            KeyId = "Zgp1LO/3y58pznVt/nwJAA==",
            PublicKey = ConvertBase64StringToByteString("MIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQB+z3l/PYeUIkSbTdnUbJ5gec5HPkRHeqOwl41qNEpJDhCzRS603uCx2A5/ulRiV6qJ30gomRZ3UERQEnqKsj66q0A5jjw691VC9U1rgNTnc0i7M2RI6j+yS9YKZbOOZ9e3O1WfHwgaDdpEphwq9/iKKf3dHwl2FexT6GARRRZQeIyGPI="),
            ValidFrom = new DateTime(2021, 12, 29, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            ValidTo = new DateTime(2021, 12, 30, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            AuthenticationTag = ConvertBase64StringToByteString("AHEFnWNJv1dvQD3GpRNfHgacAiW7bjfS6XCl0j+6PhkwHh2hpobbgj0Hv/dx2qvW953Qr4SMEwEfKiJAGFhPXjA1AcMfHynNsD+T1CgUNipc8PZ8mZMgol0Eq4NBAXMuUnIoIe7cjni8FdRFIPU+NzBVKapr3vpz4iTjz4x9lY/oAyGZ"),
        };

        hsmSignature ??= Convert.FromBase64String("AHmdWmclPaF597VdtJRMYFY+KtseA4VljNPkg0IXsMM1B6bF48y1s2xT/RqwqMo9DkphC3v5+meUbcIK9RT4K0MWAEMXs4xshA35mTVf61b2IbHSRzeq4vsAD7VvPnI6j3I4iIusLNzScZUD/VLwroUx9CG+ONflusxcl1rRxWZxrHWn");

        customizer?.Invoke(ev);
        return new PublicKeySignatureCreateData(ev.KeyId, ev.SignatureVersion, Guid.Parse(ev.ContestId), ev.HostId, ev.AuthenticationTag.ToByteArray(), hsmSignature);
    }

    private PublicKeySignatureDeleteData NewDeleteData(
        Action<EventSignaturePublicKeyDeleted>? customizer = null,
        byte[]? hsmSignature = null)
    {
        var ev = new EventSignaturePublicKeyDeleted
        {
            SignatureVersion = EventSignatureVersions.V1,
            ContestId = "e9840c66-9bd3-4950-9b22-d680fa6ba166",
            HostId = "Host",
            KeyId = "Zgp1LO/3y58pznVt/nwJAA==",
            SignedEventCount = 100,
            DeletedAt = new DateTime(2021, 12, 29, 18, 0, 0, DateTimeKind.Utc).ToTimestamp(),
            AuthenticationTag = ConvertBase64StringToByteString("AFCXTVrm9CSvOhSOIT3fRXUaKWv0hpMKnfB4UjBYBHMY0f1/tnQJeSedWQtTeREbosUbJPQs8xLpE1nVTJaQ+FxIAY7EcyVoKJNmXkwqy9ObyZKPJzrYCD4gUD+u3AUbsOBwfPnakVX80QkWxl1HJe+C8IfjkfXOCXdqyqHtaksLFstx"),
        };

        hsmSignature ??= Convert.FromBase64String("AZChPYnXpkawnQWBpuChPUK826lecevTZmvM7CtN0rMXyhjRoJwbXGjJKky2Y7HRVVx7ii+PJpo5893nK3YpQHtaAUGZkttjJ5dOdzMbHyuU75eiddbUW2vQn/eRDgoHXv38T0fSyC7FD6iEkR53XEg0JlDvnDvYxZEkQGNbG9VrzYZV");

        customizer?.Invoke(ev);
        return new PublicKeySignatureDeleteData(ev.KeyId, ev.SignatureVersion, Guid.Parse(ev.ContestId), ev.HostId, ev.SignedEventCount, ev.AuthenticationTag.ToByteArray(), hsmSignature);
    }

    private PublicKeyData NewKeyData()
    {
        var publicKey = _asymmetricAlgorithmAdapter.CreatePublicKey(
            Convert.FromBase64String("MIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQB+z3l/PYeUIkSbTdnUbJ5gec5HPkRHeqOwl41qNEpJDhCzRS603uCx2A5/ulRiV6qJ30gomRZ3UERQEnqKsj66q0A5jjw691VC9U1rgNTnc0i7M2RI6j+yS9YKZbOOZ9e3O1WfHwgaDdpEphwq9/iKKf3dHwl2FexT6GARRRZQeIyGPI="),
            "Zgp1LO/3y58pznVt/nwJAA==");

        return new PublicKeyData(
            publicKey,
            new DateTime(2021, 12, 29, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2021, 12, 30, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2021, 12, 29, 18, 0, 0, DateTimeKind.Utc));
    }

    private ByteString ConvertBase64StringToByteString(string base64)
        => ByteString.CopyFrom(Convert.FromBase64String(base64));
}
