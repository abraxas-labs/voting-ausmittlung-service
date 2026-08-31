// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Utils.ECountingSignature.Models;

internal class CryptoFile
{
    public byte[] Signature { get; set; } = Array.Empty<byte>();

    public CryptoFileContent Content { get; set; } = new();
}
