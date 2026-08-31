// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils.ECountingSignature.Models;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.ECountingSignature;

internal static class CryptoFileBuilder
{
    private static readonly string[] _enabledVersions = new[] { CryptographyConstants.V1 };

    public static CryptoFile BuildFile(byte[] fileContentBytes)
    {
        if (fileContentBytes.Length == 0)
        {
            throw new InvalidOperationException("File content bytes must not be empty");
        }

        var (textBytes, binaryBytes) = SplitTextAndBinaryBytes(fileContentBytes);

        using var msStream = new MemoryStream(textBytes);
        using var textReader = new StreamReader(msStream);

        var signatureLine = textReader.ReadLine()
            ?? throw new CryptoFileException("Expected signature but found null");

        var content = BuildContent(textReader, binaryBytes);

        return new()
        {
            Signature = Convert.FromBase64String(signatureLine),
            Content = content,
        };
    }

    private static CryptoFileContent BuildContent(StreamReader streamReader, byte[] binaryBytes)
    {
        var header = ReadHeader(streamReader);

        return new()
        {
            Header = header,
            Payload = binaryBytes,
        };
    }

    private static (byte[] TextBytes, byte[] BinaryBytes) SplitTextAndBinaryBytes(byte[] fileContentBytes)
    {
        var matchingDelimiterCharsCount = 0;
        var delimiter = Encoding.UTF8.GetBytes(CryptographyConstants.NewLine + CryptographyConstants.HeaderEndDelimiter + CryptographyConstants.NewLine);

        for (var i = 0; i < fileContentBytes.Length; i++)
        {
            if (fileContentBytes[i] == delimiter[matchingDelimiterCharsCount])
            {
                matchingDelimiterCharsCount++;
            }
            else
            {
                matchingDelimiterCharsCount = 0;
            }

            // found delimiter
            if (matchingDelimiterCharsCount == delimiter.Length)
            {
                var textBytes = new byte[i + 1];
                Buffer.BlockCopy(fileContentBytes, 0, textBytes, 0, textBytes.Length);

                var binaryBytes = new byte[fileContentBytes.Length - textBytes.Length];
                Buffer.BlockCopy(fileContentBytes, textBytes.Length, binaryBytes, 0, binaryBytes.Length);

                return (textBytes, binaryBytes);
            }
        }

        throw new CryptoFileException("Did not find the delimiter between the text and binary bytes");
    }

    private static CryptoFileHeader ReadHeader(StreamReader streamReader)
    {
        var header = new CryptoFileHeader();
        var rawTextBuilder = new StringBuilder();

        var headerStartDelimiter = streamReader.ReadLine();
        if (!string.Equals(headerStartDelimiter, CryptographyConstants.HeaderStartDelimiter, StringComparison.Ordinal))
        {
            throw new CryptoFileException($"Expected header start but found '{headerStartDelimiter}'");
        }

        rawTextBuilder.Append(headerStartDelimiter);
        rawTextBuilder.Append(CryptographyConstants.NewLine);

        var versionLine = streamReader.ReadLine();

        if (!_enabledVersions.Contains(versionLine))
        {
            throw new CryptoFileException($"Expected an enabled version but found '{versionLine}'");
        }

        header.Version = versionLine!;
        rawTextBuilder.Append(versionLine);
        rawTextBuilder.Append(CryptographyConstants.NewLine);

        var senderCertificateLine = streamReader.ReadLine();
        if (string.IsNullOrWhiteSpace(senderCertificateLine))
        {
            throw new CryptoFileException($"Expected sender certificate line but found '{senderCertificateLine}'");
        }

        X509Certificate2? senderCertificate;

        try
        {
            senderCertificate = new X509Certificate2(Convert.FromBase64String(senderCertificateLine));
        }
        catch (Exception ex)
        {
            throw new CryptoFileException("Cannot convert the sender certificate line to a X509 certificate: " + ex.Message);
        }

        header.ClientCertificateInfo = CertificateInfoBuilder.Build<ResultImportECountingClientCertificateInfo>(senderCertificate);

        rawTextBuilder.Append(senderCertificateLine);
        rawTextBuilder.Append(CryptographyConstants.NewLine);

        var nextLine = streamReader.ReadLine();
        if (!string.Equals(nextLine, CryptographyConstants.HeaderEndDelimiter, StringComparison.Ordinal))
        {
            throw new CryptoFileException("Expected header end delimiter but not found");
        }

        rawTextBuilder.Append(nextLine);
        header.RawText = rawTextBuilder.ToString();

        return header;
    }
}
