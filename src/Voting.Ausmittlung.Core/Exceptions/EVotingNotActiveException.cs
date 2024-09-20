// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Core.Exceptions;

public class EVotingNotActiveException : ValidationException
{
    public EVotingNotActiveException(string name, Guid id)
        : base($"eVoting is not active on the {name} with the id {id}")
    {
    }
}
