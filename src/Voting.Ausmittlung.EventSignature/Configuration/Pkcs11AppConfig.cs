// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Cryptography.Pkcs11.Configuration;

namespace Voting.Ausmittlung.EventSignature.Configuration;

/// <summary>
/// Extended application configuration for PKCS11.
/// </summary>
public record Pkcs11AppConfig : Pkcs11Config
{
    /// <summary>
    /// Gets or sets the CKA_LABEL which is required to get the stored Public Key of the device
    /// used for asymmetric algorithms.
    /// </summary>
    public string PublicKeyCkaLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CKA_LABEL which is required to get the stored Private Key of the device
    /// used for asymmetric algorithms.
    /// </summary>
    public string PrivateKeyCkaLabel { get; set; } = string.Empty;
}
