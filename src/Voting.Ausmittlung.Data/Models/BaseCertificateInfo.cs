// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class BaseCertificateInfo : BaseEntity
{
    public string RawPem { get; set; } = string.Empty;

    public DateTime NotBefore { get; set; }

    public DateTime NotAfter { get; set; }

    public string Issuer { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Thumbprint { get; set; } = string.Empty;

    public string CaThumbprint { get; set; } = string.Empty;
}
