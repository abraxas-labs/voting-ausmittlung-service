// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils.ECountingSignature.Models;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Cryptography.X509;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils.ECountingSignature;

public class ECountingEchFileDecryptor
{
    private const int ECountingClientCertificateRawPemMaxLength = 51200;
    private const string CommonNameOid = "2.5.4.3";
    private static readonly X509RevocationMode _x509RevocationMode = X509RevocationMode.NoCheck;
    private static readonly HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA512;

    private readonly IDbRepository<DataContext, CantonSettingsECountingCertificateChain> _cantonSettingsECountingCertificateChainRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;

    public ECountingEchFileDecryptor(
        IDbRepository<DataContext, CantonSettingsECountingCertificateChain> cantonSettingsECountingCertificateChainRepo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo)
    {
        _cantonSettingsECountingCertificateChainRepo = cantonSettingsECountingCertificateChainRepo;
        _countingCircleRepo = countingCircleRepo;
    }

    public string Version { get; } = CryptographyConstants.V1;

    public async Task<(Stream Stream, BaseCertificateInfo ClientCertificate)> VerifyAndExtractEch0222AndClientCertificate(Stream stream, Guid countingCircleId)
    {
        var (supplierCertificateChain, countingCircleBfs) = await GetCertificateChainAndBfs(countingCircleId);

        // Since e-counting imports aren't that large, we can use memory streams here.
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var cryptoFile = CryptoFileBuilder.BuildFile(ms.ToArray());
        Verify(cryptoFile, supplierCertificateChain, countingCircleBfs);

        return (
            new MemoryStream(Convert.FromBase64String(Encoding.UTF8.GetString(cryptoFile.Content.Payload))),
            cryptoFile.Content.Header.ClientCertificateInfo);
    }

    private void Verify(CryptoFile cryptoFile, CantonSettingsECountingCertificateChain supplierCertificateChain, string countingCircleBfs)
    {
        if (cryptoFile.Content.Header.Version != Version)
        {
            throw new ArgumentException($"Unsupported verison. Expected {Version}, actual {cryptoFile.Content.Header.Version}.");
        }

        var clientCertificateInfo = cryptoFile.Content.Header.ClientCertificateInfo;

        if (clientCertificateInfo.RawPem.Length > ECountingClientCertificateRawPemMaxLength)
        {
            throw new ValidationException($"Client certificate raw pem max length is {ECountingClientCertificateRawPemMaxLength}");
        }

        var clientCertificate = X509Certificate2.CreateFromPem(clientCertificateInfo.RawPem);
        var clientCertificateCommonName = GetSubjectCommonName(clientCertificate);

        if (clientCertificateCommonName != countingCircleBfs)
        {
            throw new ResultImportECountingClientCertificateCnException(clientCertificateCommonName, countingCircleBfs);
        }

        VerifyCertificateChain(clientCertificate, clientCertificateInfo, supplierCertificateChain);

        using var ecdsa = clientCertificate.GetECDsaPublicKey()
            ?? throw new ArgumentException("The client certificate does not contain an ECDSA public key.");

        if (!ecdsa.VerifyData(cryptoFile.Content.GetContentBytes(), cryptoFile.Signature, _hashAlgorithmName))
        {
            throw new ResultImportECountingSignatureVerifyException(countingCircleBfs);
        }
    }

    private void VerifyCertificateChain(
        X509Certificate2 clientCertificate,
        BaseCertificateInfo clientCertificateInfo,
        CantonSettingsECountingCertificateChain supplierCertificateChain)
    {
        var supplierCerts = new X509Certificate2Collection();
        supplierCerts.ImportFromPem(supplierCertificateChain.RawPem);
        supplierCerts.Add(clientCertificate);

        var chainElements = X509ChainBuilder.VerifyLinearValidCertificateChain(supplierCerts, _x509RevocationMode);
        var caThumbprint = CertificateInfoBuilder.BuildThumbprintSha256(chainElements[1]);

        if (!supplierCertificateChain.Infos!.Any(i => i.Thumbprint == caThumbprint))
        {
            throw new ValidationException($"The CA thumbprint {caThumbprint} was not found in the existing certificate chain");
        }

        clientCertificateInfo.CaThumbprint = caThumbprint;
    }

    private string GetSubjectCommonName(X509Certificate2 cert)
    {
        var cnElement = cert.SubjectName.EnumerateRelativeDistinguishedNames()
            .FirstOrDefault(rdn => !rdn.HasMultipleElements && rdn.GetSingleElementType().Value == CommonNameOid);

        return cnElement!.GetSingleElementValue()!;
    }

    private async Task<(CantonSettingsECountingCertificateChain CertificateChain, string Bfs)> GetCertificateChainAndBfs(Guid countingCircleId)
    {
        var countingCircle = await _countingCircleRepo
            .Query()
            .Include(cc => cc.DomainOfInfluences)
            .ThenInclude(doiCc => doiCc.DomainOfInfluence)
            .FirstOrDefaultAsync(c => c.Id == countingCircleId)
            ?? throw new EntityNotFoundException(nameof(CountingCircle), countingCircleId);

        var canton = countingCircle.DomainOfInfluences
            .Select(doiCc => doiCc.DomainOfInfluence.Canton)
            .FirstOrDefault();

        var certificateChain = await _cantonSettingsECountingCertificateChainRepo.Query()
            .Where(c => c.CantonSettings!.Canton == canton)
            .Include(c => c.Infos)
            .OrderByDescending(c => c.ImportedAt)
            .FirstOrDefaultAsync() ?? throw new EntityNotFoundException(nameof(CantonSettingsECountingCertificateChain), canton);

        return (certificateChain, countingCircle.Bfs);
    }
}
