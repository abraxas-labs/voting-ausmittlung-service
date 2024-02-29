// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ContestCountingCircleDetailsNotUpdatableException : Exception
{
    public ContestCountingCircleDetailsNotUpdatableException()
        : base("The contest counting circle details cannot be updated, since a political business is already finished")
    {
    }
}
