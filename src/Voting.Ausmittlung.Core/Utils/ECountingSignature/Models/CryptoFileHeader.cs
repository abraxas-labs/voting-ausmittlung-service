// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.ECountingSignature.Models;

internal class CryptoFileHeader
{
    public string Version { get; set; } = string.Empty;

    public BaseCertificateInfo ClientCertificateInfo { get; set; } = null!;

    public string RawText { get; set; } = string.Empty;
}
