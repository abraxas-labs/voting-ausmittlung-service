// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Security.Cryptography.X509Certificates;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Cryptography.X509;

namespace Voting.Ausmittlung.Core.Utils;

public static class CertificateChainBuilder
{
    private static readonly X509RevocationMode _x509RevocationMode = X509RevocationMode.NoCheck;

    public static TCertificateChain Build<TCertificateChain, TCertificateInfo>(string rawPem, User user, DateTime importedAt)
        where TCertificateChain : BaseCertificateChain<TCertificateInfo>, new()
        where TCertificateInfo : BaseCertificateInfo, new()
    {
        var certificateCollection = new X509Certificate2Collection();
        certificateCollection.ImportFromPem(rawPem);

        var orderedVerifiedCertificates = X509ChainBuilder.VerifyLinearValidCertificateChain(certificateCollection, _x509RevocationMode);
        var orderedCertificateInfos = orderedVerifiedCertificates.ConvertAll(e => CertificateInfoBuilder.Build<TCertificateInfo>(e));

        for (var i = 0; i < orderedCertificateInfos.Count - 1; i++)
        {
            var certificateInfo = orderedCertificateInfos[i];
            var caCertificateInfo = orderedCertificateInfos[i + 1];
            certificateInfo.CaThumbprint = caCertificateInfo.Thumbprint;
        }

        return new()
        {
            RawPem = rawPem,
            Infos = orderedCertificateInfos,
            ImportedBy = user,
            ImportedAt = importedAt,
        };
    }
}
