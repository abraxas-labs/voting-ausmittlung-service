// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(object id)
        : base($"Entity with id {id} not found")
    {
    }

    public EntityNotFoundException(string name, object id)
        : base($"{name} with id {id} not found")
    {
    }
}
