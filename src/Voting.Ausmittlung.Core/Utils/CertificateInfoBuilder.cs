// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils;

public static class CertificateInfoBuilder
{
    public static TCertificateInfo Build<TCertificateInfo>(X509Certificate2 certificate)
        where TCertificateInfo : BaseCertificateInfo, new()
    {
        return new()
        {
            RawPem = certificate.ExportCertificatePem(),
            NotBefore = certificate.NotBefore.ToUniversalTime(),
            NotAfter = certificate.NotAfter.ToUniversalTime(),
            Issuer = certificate.IssuerName.Name,
            Subject = certificate.SubjectName.Name,
            Thumbprint = BuildThumbprintSha256(certificate),
        };
    }

    public static string BuildThumbprintSha256(X509Certificate2 certificate)
    {
        var sha256Hash = SHA256.HashData(certificate.RawData);
        return BitConverter.ToString(sha256Hash).Replace("-", string.Empty).ToUpperInvariant();
    }
}
