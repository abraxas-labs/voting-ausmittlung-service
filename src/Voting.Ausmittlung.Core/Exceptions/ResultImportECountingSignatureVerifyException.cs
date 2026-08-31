// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ResultImportECountingSignatureVerifyException : Exception
{
    public ResultImportECountingSignatureVerifyException(string countingCircleBfs)
        : base($"E-Counting import signature verification failed for counting circle {countingCircleBfs}")
    {
    }
}
