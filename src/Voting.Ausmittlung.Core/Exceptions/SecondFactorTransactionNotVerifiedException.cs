// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Core.Exceptions;

public class SecondFactorTransactionNotVerifiedException : ValidationException
{
    public SecondFactorTransactionNotVerifiedException()
        : base("Second factor transaction is not verified")
    {
    }
}
