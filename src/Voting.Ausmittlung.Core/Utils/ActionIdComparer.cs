// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Utils;

public class ActionIdComparer : IActionIdComparer
{
    public bool Compare(ActionId actionId, string actionIdHash)
    {
        var computedActionIdHash = actionId.ComputeHash();
        return computedActionIdHash.Equals(actionIdHash, StringComparison.Ordinal);
    }
}
