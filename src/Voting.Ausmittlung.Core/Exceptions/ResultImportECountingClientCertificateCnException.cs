// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ResultImportECountingClientCertificateCnException : Exception
{
    public ResultImportECountingClientCertificateCnException(string clientCertificateCommonName, string countingCircleBfs)
        : base($"The client certificate CN {clientCertificateCommonName} does not match the counting circle bfs {countingCircleBfs}")
    {
    }
}
