// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;

namespace Voting.Ausmittlung.Test.MockedData.TestFiles;

public static class CertificateTestFiles
{
    public static string SgSupplierCertRawPem => GetCertificateRawPem("sg-supplier.ca.pub.pem");

    public static string ClientCert1234RawPem => GetCertificateRawPem("sender.client.1234.pub.pem");

    public static string FourLevelChainRawPem => GetCertificateRawPem("four-level-chain.pem");

    public static string FourLevelChainWithMultiLeafRawPem => GetCertificateRawPem("four-level-chain-with-multi-leaf.pem");

    private static string GetCertificateRawPem(string fileName)
    {
        var assemblyFolder = Path.GetDirectoryName(typeof(CertificateTestFiles).Assembly.Location);
        return File.ReadAllText(Path.Join(assemblyFolder, $"MockedData/TestFiles/{fileName}"));
    }
}
