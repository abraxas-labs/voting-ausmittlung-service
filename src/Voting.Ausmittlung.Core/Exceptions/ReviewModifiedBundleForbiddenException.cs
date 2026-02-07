// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ReviewModifiedBundleForbiddenException : Exception
{
    public ReviewModifiedBundleForbiddenException()
        : base("The user cannot succeed the review of a bundle that he modified himself")
    {
    }
}
