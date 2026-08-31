// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Text;

namespace Voting.Ausmittlung.Core.Utils.ECountingSignature.Models;

internal class CryptoFileContent
{
    public CryptoFileHeader Header { get; set; } = new();

    public byte[] Payload { get; set; } = Array.Empty<byte>();

    public string RawText { get; set; } = string.Empty;

    public byte[] GetContentBytes()
    {
        return Encoding.UTF8.GetBytes(Header.RawText + CryptographyConstants.NewLine + Encoding.UTF8.GetString(Payload));
    }
}
