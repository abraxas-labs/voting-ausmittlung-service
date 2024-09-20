// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.EventSignature.Models;

public class PublicKeySignatureData
{
    public PublicKeySignatureData(int signatureVersion, string hostId)
    {
        SignatureVersion = signatureVersion;
        HostId = hostId;
    }

    public int SignatureVersion { get; }

    public string HostId { get; }
}
