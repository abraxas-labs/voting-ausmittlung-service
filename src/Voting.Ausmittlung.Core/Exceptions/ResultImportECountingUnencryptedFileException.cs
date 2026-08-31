// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ResultImportECountingUnencryptedFileException : Exception
{
    public ResultImportECountingUnencryptedFileException()
        : base("E-Counting imports require an encrypted eCH-0222 file")
    {
    }
}
