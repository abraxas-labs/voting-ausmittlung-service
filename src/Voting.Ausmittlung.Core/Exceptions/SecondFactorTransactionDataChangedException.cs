// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Core.Exceptions;

public class SecondFactorTransactionDataChangedException : ValidationException
{
    public SecondFactorTransactionDataChangedException()
        : base("Data changed during the second factor transaction")
    {
    }
}
