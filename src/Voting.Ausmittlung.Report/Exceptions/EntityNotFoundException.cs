// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string name, object id)
        : base($"{name} with id {id} not found")
    {
    }
}
